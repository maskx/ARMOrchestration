using maskx.ARMOrchestration.Orchestrations;
using maskx.DurableTask.SQLServer.SQL;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Workers
{
    public class WaitDependsOnWorker : BackgroundService
    {
        private TemplateOrchestrationOptions options;

        public WaitDependsOnWorker(IOptions<TemplateOrchestrationOptions> options)
        {
            this.options = options?.Value;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await this.CreateIfNotExistsAsync(false);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
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
        [InstanceId] [nvarchar](50) NOT NULL,
	    [ExecutionId] [nvarchar](50) NOT NULL,
        [DependsOnName] [nvarchar](500) NOT NULL,
        [DependsOnStatus] [nvarchar](50) NOT NULL,
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
    }
}