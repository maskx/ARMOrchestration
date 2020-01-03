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

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public async Task DeleteARMOrchestrationTableAsync()
        {
            using (var db = new DbAccess(options.Database.ConnectionString))
            {
                db.AddStatement($"DROP TABLE IF EXISTS {options.Database.WaitDependsOnTableName}");
                db.AddStatement($"DROP TABLE IF EXISTS {options.Database.DeploymentDetailTableName}");
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
    ) END", new { table = options.Database.WaitDependsOnTableName });
                db.AddStatement($@"
IF(OBJECT_ID(@table) IS NULL)
BEGIN
    create table {options.Database.DeploymentDetailTableName}(
        [DeploymentId] [nvarchar](50) NOT NULL,
	    [Resource] [nvarchar](50) NOT NULL,
	    [Type] [nvarchar](100) NULL,
	    [Status] [nvarchar](20) NULL,
	    [ResourceId] [nvarchar](500) NULL,
	    [ParentId] [nvarchar](500) NULL,
	    [BeginTimeUtc] [datetime2](7) NULL,
	    [EndTimeUtc] [datetime2](7) NULL,
	    [InstanceId] [nvarchar](50) NULL,
	    [ExecutionId] [nvarchar](50) NULL,
        [Result] [nvarchar](500) NULL
    ) END", new { table = options.Database.DeploymentDetailTableName });
                await db.ExecuteNonQueryAsync();
            }
        }
    }
}