﻿using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Workers
{
    public class WaitDependsOnWorker<T> : BackgroundService where T : CommunicationJob, new()
    {
        private readonly ARMOrchestrationOptions options;
        private readonly TaskHubClient taskHubClient;
        private readonly DataConverter dataConverter = new JsonDataConverter();
        private readonly IInfrastructure _Infrastructure;
        private readonly ILoggerFactory _LoggerFactory;

        public WaitDependsOnWorker(
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<ARMOrchestrationOptions> options,
            OrchestrationWorker orchestrationWorker,
            IInfrastructure infrastructure,
            ILoggerFactory loggerFactory)
        {
            this._LoggerFactory = loggerFactory;
            this.options = options?.Value;
            this._Infrastructure = infrastructure;
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
            this.fetchCommandString = string.Format(fetchCommandTemplate,
                this.options.Database.WaitDependsOnTableName,
                this.options.Database.DeploymentOperationsTableName,
                (int)ProvisioningStage.Successed);
            this.removeCommandString = string.Format(removeCommandTemplate, this.options.Database.WaitDependsOnTableName);
            orchestrationWorker.AddActivity(typeof(WaitDependsOnActivity), WaitDependsOnActivity.Name, "1.0");
            orchestrationWorker.AddActivity(typeof(AsyncRequestActivity<T>), AsyncRequestActivity<T>.Name, "1.0");
            orchestrationWorker.AddOrchestration(typeof(DeploymentOrchestration<T>), DeploymentOrchestration<T>.Name, "1.0");
            orchestrationWorker.AddOrchestration(typeof(ResourceOrchestration<T>), ResourceOrchestration<T>.Name, "1.0");
            orchestrationWorker.AddOrchestration(typeof(RequestOrchestration<T>), RequestOrchestration<T>.Name, "1.0");
            orchestrationWorker.AddOrchestration(typeof(CopyOrchestration<T>), CopyOrchestration<T>.Name, "1.0");
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (this.options.Database.AutoCreate)
                await this.CreateIfNotExistsAsync(false);
            await base.StartAsync(cancellationToken);
        }

        private async Task<List<(string InstanceId, string ExecutionId, string EventName, int FailCount)>> GetResolvedDependsOn()
        {
            List<(string InstanceId, string ExecutionId, string EventName, int FailCount)> rtv = new List<(string InstanceId, string ExecutionId, string EventName, int FailCount)>();
            using (var db = new SQLServerAccess(this.options.Database.ConnectionString,_LoggerFactory))
            {
                db.AddStatement(this.fetchCommandString);
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    rtv.Add((
                        reader["InstanceId"].ToString(),
                        reader["ExecutionId"].ToString(),
                        reader["EventName"].ToString(),
                        reader.GetInt32(3)));
                });
            }
            return rtv;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    List<Task> tasks = new List<Task>();
                    foreach (var (InstanceId, ExecutionId, EventName, FailCount) in await GetResolvedDependsOn())
                    {
                        tasks.Add(ResolveDependsOn(InstanceId, ExecutionId, EventName, FailCount));
                    }
                    await Task.WhenAll(tasks.ToArray());
                    if (tasks.Count == 0)
                        await Task.Delay(this.options.DependsOnIdelMilliseconds);
                }
                catch (Exception ex)
                {
                    _LoggerFactory.CreateLogger<WaitDependsOnWorker<T>>().LogError($"WaitDependsOnWorker execute error:{ex.Message};{ex.StackTrace}");
                }
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // todo:logging, this worker should not be stoped
            return base.StopAsync(cancellationToken);
        }
        private async Task ResolveDependsOn(string instanceId, string executionId, string eventName, int failCount)
        {
            // This may be raised multiple times when multiple instances are running
            // so in orchestartion need to verify this.waitHandler.Task.Status == TaskStatus.WaitingForActivation
            await this.taskHubClient.RaiseEventAsync(
                                           new OrchestrationInstance()
                                           {
                                               InstanceId = instanceId,
                                               ExecutionId = executionId
                                           },
                                           eventName,
                                           dataConverter.Serialize(failCount > 0 ? new TaskResult(500, "One of dependsOn resources has failed") : new TaskResult(200, null))
                                           );
            using var db = new SQLServerAccess(this.options.Database.ConnectionString,_LoggerFactory);
            db.AddStatement(this.removeCommandString,
               new
               {
                   InstanceId = instanceId,
                   ExecutionId = executionId,
                   EventName = eventName
               });
            await db.ExecuteNonQueryAsync();
        }

        public async Task DeleteARMOrchestrationTableAsync()
        {
            using var db = new SQLServerAccess(options.Database.ConnectionString,_LoggerFactory);
            db.AddStatement($"DROP TABLE IF EXISTS {options.Database.WaitDependsOnTableName}");
            db.AddStatement($"DROP TABLE IF EXISTS {options.Database.DeploymentOperationsTableName}");
            await db.ExecuteNonQueryAsync();
        }

        public async Task CreateIfNotExistsAsync(bool recreate)
        {
            if (recreate) await DeleteARMOrchestrationTableAsync();
            using (var db = new SQLServerAccess(options.Database.ConnectionString,_LoggerFactory))
            {
                db.AddStatement($@"IF(SCHEMA_ID('{options.Database.SchemaName}') IS NULL)
                    BEGIN
                       EXEC('CREATE SCHEMA [{options.Database.SchemaName}]')
                    END");

                db.AddStatement($@"
IF(OBJECT_ID('{options.Database.WaitDependsOnTableName}') IS NULL)
BEGIN
    CREATE TABLE {options.Database.WaitDependsOnTableName} (
        [RootId] [nvarchar](50) NOT NULL,
        [DeploymentId] [nvarchar](50) NOT NULL,
        [InstanceId] [nvarchar](50) NOT NULL,
	    [ExecutionId] [nvarchar](50) NOT NULL,
        [EventName] [nvarchar](50) NOT NULL,
        [DependsOnName] [nvarchar](500) NOT NULL,
	    [CompletedTime] [datetime2](7) NULL,
	    [CreateTime] [datetime2](7) NULL
    )
END");
                db.AddStatement($@"
IF(OBJECT_ID('{options.Database.DeploymentOperationsTableName}') IS NULL)
BEGIN
    create table {options.Database.DeploymentOperationsTableName}(
        [Id] [nvarchar](50) NOT NULL,
        [DeploymentId] [nvarchar](50) NOT NULL,
        [InstanceId] [nvarchar](50) NOT NULL,
	    [RootId] [nvarchar](50) NOT NULL,
        [CorrelationId] [nvarchar](50) NOT NULL,
	    [GroupId] [nvarchar](50) NOT NULL,
        [GroupType] [nvarchar](50) NOT NULL,
        [HierarchyId] [nvarchar](1024) NOT NULL,
        [ResourceId] [nvarchar](1024) NOT NULL,
        [Name] [nvarchar](1024) NOT NULL,
	    [Type] [nvarchar](200) NOT NULL,
	    [Stage] [int] NOT NULL,
	    [CreateTimeUtc] [datetime2](7) NOT NULL,
	    [UpdateTimeUtc] [datetime2](7) NOT NULL,
        [CreateByUserId] [nvarchar](50) NOT NULL,
        [LastRunUserId] [nvarchar](50) NOT NULL,
        [ApiVersion] [nvarchar](50) NOT NULL,
        [ExecutionId] [nvarchar](50)  NULL,
        [Comments] [nvarchar](256) NULL,
        [SubscriptionId] [nvarchar](50)  NULL,
        [ManagementGroupId] [nvarchar](50)  NULL,
        [ParentResourceId] [nvarchar](1024)  NULL,
        [Input] [nvarchar](max) NULL,
	    [Result] [nvarchar](max) NULL,
     CONSTRAINT [PK_{options.Database.SchemaName}_{options.Database.HubName}_{DatabaseConfig.DeploymentOperationsTable}] PRIMARY KEY CLUSTERED
    (
	    [Id] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
END");

                db.AddStatement($@"
IF(OBJECT_ID('{options.Database.DeploymentOperationHistoryTableName}') IS NULL)
BEGIN
    CREATE TABLE {options.Database.DeploymentOperationHistoryTableName}(
	    [DeploymentOperationId] [nvarchar](50) NOT NULL,
	    [InstanceId] [nvarchar](50) NOT NULL,
	    [ExecutionId] [nvarchar](50) NOT NULL,
        [LastRunUserId] [nvarchar](50) NOT NULL,
        [UpdateTimeUtc] [datetime2](7) NOT NULL,
     CONSTRAINT [PK_{options.Database.SchemaName}_{options.Database.HubName}_{DatabaseConfig.DeploymentOperationHistoryTable}] PRIMARY KEY CLUSTERED 
    (
	    [DeploymentOPerationId] ASC,
	    [InstanceId] ASC,
	    [ExecutionId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY]
END");

                await db.ExecuteNonQueryAsync();
            }
#pragma warning disable IDE0063 // Use simple 'using' statement
            using (var db2 = new SQLServerAccess(options.Database.ConnectionString,_LoggerFactory))
#pragma warning restore IDE0063 // Use simple 'using' statement
            {
                db2.AddStatement($@"
CREATE OR ALTER PROCEDURE {options.Database.RetrySPName} 
	@Id nvarchar(50),
	@NewInstanceId nvarchar(50),
	@NewExecutionId nvarchar(50),
	@LastRunUserId nvarchar(50),
	@Input nvarchar(max) =null
AS
BEGIN

	SET NOCOUNT ON;
	declare @InstanceId nvarchar(50)=null
	declare @ExecutionId nvarchar(50)=null
    declare @UpdateTimeUtc [datetime2](7)
    declare @RunUserId nvarchar(50)
	declare @Type nvarchar(50)
	declare @ParentResourceId nvarchar(1024)

	update {options.Database.DeploymentOperationsTableName}
	set	InstanceId=@NewInstanceId,
		ExecutionId=@NewExecutionId,
		Stage={(int)ProvisioningStage.StartProvisioning},
		LastRunUserId=@LastRunUserId,
		Input=isnull(@Input,Input),
		@InstanceId=InstanceId, 
		@ExecutionId=ExecutionId,
        @UpdateTimeUtc=UpdateTimeUtc,
        @RunUserId=LastRunUserId,
        @Type=[Type],
		@ParentResourceId=ParentResourceId
	where Id=@Id and (Stage={(int)ProvisioningStage.Failed} or Stage={(int)ProvisioningStage.Pending})
	if @@ROWCOUNT=1
    BEGIN
        insert into {options.Database.DeploymentOperationHistoryTableName} values(@Id,@InstanceId,@ExecutionId,@RunUserId,@UpdateTimeUtc)
        if @Type=N'{_Infrastructure.BuiltinServiceTypes.Deployments}'
		begin
			update {options.Database.DeploymentOperationsTableName} 
			set Stage={(int)ProvisioningStage.Pending}
			where  [ParentResourceId]=@ParentResourceId and Stage={(int)ProvisioningStage.Failed}
		end
    END
select Stage from {options.Database.DeploymentOperationsTableName} where Id=@Id
END
");
                await db2.ExecuteNonQueryAsync();
            }
        }

        private readonly string removeCommandString;

        private const string removeCommandTemplate = @"
delete {0} where InstanceId=@InstanceId and ExecutionId=@ExecutionId and EventName=@EventName
";

        private readonly string fetchCommandString;

        /// <summary>
        /// {0}: WaitDependsOn table name
        /// {1}: DeploymentOperations
        /// {2}: ResourceCommitSuccessed
        /// </summary>
        private const string fetchCommandTemplate = @"
select t.InstanceId,t.ExecutionId,t.EventName,t.FailCount
from(
	select
		w.InstanceId
		, w.ExecutionId
        ,w.EventName
		,COUNT(0) OVER (PARTITION BY w.InstanceId , w.ExecutionId,w.EventName) as WaitCount
		,COUNT(case when d.Stage={2} then 1 else null end) OVER (PARTITION BY w.InstanceId , w.ExecutionId,w.EventName) as SuccessCount
        ,COUNT(case when d.Stage<0 then 1 else null end) OVER (PARTITION BY w.InstanceId , w.ExecutionId,w.EventName) as FailCount
	from {0} as w
		left join {1} as d
			on w.RootId=d.RootId and d.ResourceId like N'%'+w.DependsOnName
) as t
where t.WaitCount=t.SuccessCount or FailCount>0
";
    }
}