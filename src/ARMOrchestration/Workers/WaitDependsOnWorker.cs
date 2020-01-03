using DurableTask.Core;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Workers
{
    public class WaitDependsOnWorker : BackgroundService
    {
        private WaitDependsOnWorkerOptions options;

        public WaitDependsOnWorker(IOptions<WaitDependsOnWorkerOptions> options)
        {
            this.options = options?.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }

        public async Task DeleteCommunicationAsync()
        {
            using (var db = new DbAccess(options.ConnectionString))
            {
                db.AddStatement($"DROP TABLE IF EXISTS {options.WaitDependsOnTableName}");
                await db.ExecuteNonQueryAsync();
            }
        }

        public async Task CreateIfNotExistsAsync(bool recreate)
        {
            if (recreate) await DeleteCommunicationAsync();
            using (var db = new DbAccess(options.ConnectionString))
            {
                db.AddStatement($@"IF(SCHEMA_ID(@schema) IS NULL)
                    BEGIN
                        EXEC sp_executesql N'CREATE SCHEMA [{options.SchemaName}]'
                    END", new { schema = options.SchemaName });

                db.AddStatement($@"
IF(OBJECT_ID(@table) IS NULL)
BEGIN
    CREATE TABLE {options.WaitDependsOnTableName} (
        [InstanceId] [nvarchar](50) NOT NULL,
	    [ExecutionId] [nvarchar](50) NOT NULL,
        [ResourceId] [nvarchar](50) NOT NULL,
        [ResourceStatus] [nvarchar](50) NOT NULL,
	    [CompletedTime] [datetime2](7) NULL,
	    [CreateTime] [datetime2](7) NULL,
    CONSTRAINT [PK_{options.SchemaName}_{options.HubName}{WaitDependsOnWorkerOptions.WaitDependsOnTable}] PRIMARY KEY CLUSTERED
    (
	    [InstanceId] ASC,
	    [ExecutionId] ASC,
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END", new { table = options.WaitDependsOnTableName });

                await db.ExecuteNonQueryAsync();
            }
        }
    }
}