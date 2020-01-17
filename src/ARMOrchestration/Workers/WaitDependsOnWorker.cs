using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Workers
{
    public class WaitDependsOnWorker : BackgroundService
    {
        private TemplateOrchestrationOptions options;
        private readonly TaskHubClient taskHubClient;
        private DataConverter dataConverter = new JsonDataConverter();

        public WaitDependsOnWorker(
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<TemplateOrchestrationOptions> options)
        {
            this.options = options?.Value;
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
            this.fetchCommandString = string.Format(fetchCommandTemplate,
                this.options.Database.WaitDependsOnTableName,
                this.options.Database.DeploymentOperationsTableName,
                (int)ProvisioningStage.ResourceCommitSuccessed,
                (int)ProvisioningStage.ConditionCheckFailed);
            this.removeCommandString = string.Format(removeCommandTemplate, this.options.Database.WaitDependsOnTableName);
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.CreateIfNotExistsAsync(false);
            await base.StartAsync(cancellationToken);
        }

        private async Task<List<(string InstanceId, string ExecutionId)>> GetResolvedDependsOn()
        {
            List<(string InstanceId, string ExecutionId)> rtv = new List<(string InstanceId, string ExecutionId)>();
            using (var db = new DbAccess(this.options.Database.ConnectionString))
            {
                db.AddStatement(this.fetchCommandString);
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    rtv.Add((reader["InstanceId"].ToString(), reader["ExecutionId"].ToString()));
                });
            }
            return rtv;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                List<Task> tasks = new List<Task>();
                foreach (var item in await GetResolvedDependsOn())
                {
                    tasks.Add(ResolveDependsOn(item.InstanceId, item.ExecutionId));
                }
                await Task.WhenAll(tasks.ToArray());
                if (tasks.Count == 0)
                    await Task.Delay(this.options.DependsOnIdelMilliseconds);
            }
        }

        private async Task ResolveDependsOn(string instanceId, string executionId)
        {
            await this.taskHubClient.RaiseEventAsync(
                                           new OrchestrationInstance()
                                           {
                                               InstanceId = instanceId,
                                               ExecutionId = executionId
                                           },
                                           WaitDependsOnOrchestration.eventName,
                                           dataConverter.Serialize(new TaskResult() { Code = 200 })
                                           );
            using (var db = new DbAccess(this.options.Database.ConnectionString))
            {
                db.AddStatement(this.removeCommandString,
                   new
                   {
                       InstanceId = instanceId,
                       ExecutionId = executionId
                   });
                await db.ExecuteNonQueryAsync();
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        public async Task DeleteARMOrchestrationTableAsync()
        {
            using (var db = new DbAccess(options.Database.ConnectionString))
            {
                db.AddStatement($"DROP TABLE IF EXISTS {options.Database.WaitDependsOnTableName}");
                db.AddStatement($"DROP TABLE IF EXISTS {options.Database.DeploymentOperationsTableName}");
                await db.ExecuteNonQueryAsync();
            }
        }

        public async Task CreateIfNotExistsAsync(bool recreate)
        {
            if (recreate) await DeleteARMOrchestrationTableAsync();
            using (var db = new DbAccess(options.Database.ConnectionString))
            {
                db.AddStatement($@"IF(SCHEMA_ID(@schema) IS NULL)
                    BEGIN
                        EXEC sp_executesql N'CREATE SCHEMA [{options.Database.SchemaName}]'
                    END", new { schema = options.Database.SchemaName });

                db.AddStatement($@"
IF(OBJECT_ID(@table) IS NULL)
BEGIN
    CREATE TABLE {options.Database.WaitDependsOnTableName} (
        [DeploymentId] [nvarchar](50) NOT NULL,
        [InstanceId] [nvarchar](50) NOT NULL,
	    [ExecutionId] [nvarchar](50) NOT NULL,
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
	    [DeploymentId] [nvarchar](50) NOT NULL,
        [CorrelationId] [nvarchar](50) NOT NULL,
	    [ParentId] [nvarchar](500) NULL,
	    [Resource] [nvarchar](50) NOT NULL,
	    [Type] [nvarchar](100) NOT NULL,
	    [Stage] [int] NOT NULL,
	    [ResourceId] [nvarchar](500) NOT NULL,
	    [CreateTimeUtc] [datetime2](7) NOT NULL,
	    [UpdateTimeUtc] [datetime2](7) NOT NULL,
	    [Result] [nvarchar](500) NULL,
     CONSTRAINT [PK_{options.Database.SchemaName}_{options.Database.HubName}_{TemplateOrchestrationOptions.DatabaseConfig.DeploymentOperationsTable}] PRIMARY KEY CLUSTERED
    (
	    [InstanceId] ASC,
	    [ExecutionId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
END", new { table = options.Database.DeploymentOperationsTableName });
                await db.ExecuteNonQueryAsync();
            }
        }

        private readonly string removeCommandString;

        private const string removeCommandTemplate = @"
delete {0} where InstanceId=@InstanceId and ExecutionId=@ExecutionId
";

        private readonly string fetchCommandString;

        /// <summary>
        /// {0}: WaitDependsOn table name
        /// {1}: DeploymentOperations
        /// {2}: ResourceCommitSuccessed
        /// {3}: ConditionCheckFailed
        /// </summary>
        private const string fetchCommandTemplate = @"
select t.InstanceId,t.ExecutionId
from(
	select
		w.InstanceId
		, w.ExecutionId
		,COUNT(0) OVER (PARTITION BY w.InstanceId , w.ExecutionId) as count1
		,COUNT(d.Stage) OVER (PARTITION BY w.InstanceId , w.ExecutionId) as count2
	from {0} as w
		left join {1} as d
			on w.DeploymentId=d.DeploymentId
                and (d.Stage>{2} or d.stage={3})
                and d.ResourceId like N'%'+w.DependsOnName
) as t
where t.count1=t.count2
";
    }
}