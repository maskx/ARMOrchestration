using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.ARMOrchestration.Activities;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Workers
{
    public class WaitDependsOnWorker : BackgroundService
    {
        private readonly ARMOrchestrationOptions options;
        private readonly TaskHubClient taskHubClient;
        private readonly DataConverter dataConverter = new JsonDataConverter();

        public WaitDependsOnWorker(
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<ARMOrchestrationOptions> options)
        {
            this.options = options?.Value;
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
            this.fetchCommandString = string.Format(fetchCommandTemplate,
                this.options.Database.WaitDependsOnTableName,
                this.options.Database.DeploymentOperationsTableName,
                (int)ProvisioningStage.Successed);
            this.removeCommandString = string.Format(removeCommandTemplate, this.options.Database.WaitDependsOnTableName);
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.CreateIfNotExistsAsync(false);
            await base.StartAsync(cancellationToken);
        }

        private async Task<List<(string InstanceId, string ExecutionId, string EventName, int FailCount)>> GetResolvedDependsOn()
        {
            List<(string InstanceId, string ExecutionId, string EventName, int FailCount)> rtv = new List<(string InstanceId, string ExecutionId, string EventName, int FailCount)>();
            using (var db = new DbAccess(this.options.Database.ConnectionString))
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
                    //todo: make sure loop does not stop by any exception 
                    //logging
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
            await this.taskHubClient.RaiseEventAsync(
                                           new OrchestrationInstance()
                                           {
                                               InstanceId = instanceId,
                                               ExecutionId = executionId
                                           },
                                           eventName,
                                           dataConverter.Serialize(new TaskResult() { Code = failCount > 0 ? 500 : 200 })
                                           );
            using var db = new DbAccess(this.options.Database.ConnectionString);
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
            using var db = new DbAccess(options.Database.ConnectionString);
            db.AddStatement($"DROP TABLE IF EXISTS {options.Database.WaitDependsOnTableName}");
            db.AddStatement($"DROP TABLE IF EXISTS {options.Database.DeploymentOperationsTableName}");
            await db.ExecuteNonQueryAsync();
        }

        public async Task CreateIfNotExistsAsync(bool recreate)
        {
            if (recreate) await DeleteARMOrchestrationTableAsync();
            using var db = new DbAccess(options.Database.ConnectionString);
            db.AddStatement($@"IF(SCHEMA_ID(@schema) IS NULL)
                    BEGIN
                        EXEC sp_executesql N'CREATE SCHEMA [{options.Database.SchemaName}]'
                    END", new { schema = options.Database.SchemaName });

            db.AddStatement($@"
IF(OBJECT_ID(@table) IS NULL)
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
END", new { table = options.Database.WaitDependsOnTableName });
            db.AddStatement($@"
IF(OBJECT_ID(@table) IS NULL)
BEGIN
    create table {options.Database.DeploymentOperationsTableName}(
        [InstanceId] [nvarchar](50) NOT NULL,
	    [ExecutionId] [nvarchar](50) NOT NULL,
        [GroupId] [nvarchar](50) NOT NULL,
        [GroupType] [nvarchar](50) NOT NULL,
        [HierarchyId] [nvarchar](1024) NOT NULL,
        [RootId] [nvarchar](50) NOT NULL,
        [DeploymentId] [nvarchar](50) NOT NULL,
        [CorrelationId] [nvarchar](50) NOT NULL,
        [ResourceId] [nvarchar](1024) NOT NULL,
	    [Name] [nvarchar](1024) NOT NULL,
	    [Type] [nvarchar](200) NOT NULL,
	    [Stage] [int] NOT NULL,
	    [CreateTimeUtc] [datetime2](7) NOT NULL,
	    [UpdateTimeUtc] [datetime2](7) NOT NULL,
        [CreateByUserId] [nvarchar](50) NOT NULL,
        [LastRunUserId] [nvarchar](50) NOT NULL,
        [SubscriptionId] [nvarchar](50)  NULL,
        [ManagementGroupId] [nvarchar](50)  NULL,
        [ParentResourceId] [nvarchar](1024)  NULL,
        [Input] [nvarchar](max) NULL,
	    [Result] [nvarchar](max) NULL,
        [Comments] [nvarchar](max) NULL,
     CONSTRAINT [PK_{options.Database.SchemaName}_{options.Database.HubName}_{DatabaseConfig.DeploymentOperationsTable}] PRIMARY KEY CLUSTERED
    (
	    [InstanceId] ASC,
	    [ExecutionId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
END", new { table = options.Database.DeploymentOperationsTableName });
            await db.ExecuteNonQueryAsync();
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