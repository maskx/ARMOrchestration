using DurableTask.Core;
using DurableTask.Core.Serializing;
using DurableTask.Core.Tracing;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.Utilities;
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

        public WaitDependsOnWorker(
            IOrchestrationServiceClient orchestrationServiceClient,
            IOptions<ARMOrchestrationOptions> options,
            OrchestrationWorker orchestrationWorker,
            IInfrastructure infrastructure)
        {
            this._Infrastructure = infrastructure;
            this.options = options?.Value;
            this.taskHubClient = new TaskHubClient(orchestrationServiceClient);
            this.fetchCommandString = string.Format(fetchCommandTemplate,
                this.options.Database.WaitDependsOnTableName,
                this.options.Database.DeploymentOperationsTableName,
                (int)ProvisioningStage.Successed);
            this.removeCommandString = string.Format(removeCommandTemplate, this.options.Database.WaitDependsOnTableName);
            orchestrationWorker.AddActivity(typeof(AsyncRequestActivity<T>), AsyncRequestActivity<T>.Name, "1.0");
            orchestrationWorker.AddOrchestration(typeof(DeploymentOrchestration<T>), DeploymentOrchestration<T>.Name, "1.0");
            orchestrationWorker.AddOrchestration(typeof(ResourceOrchestration<T>), ResourceOrchestration<T>.Name, "1.0");
            orchestrationWorker.AddOrchestration(typeof(RequestOrchestration<T>), RequestOrchestration<T>.Name, "1.0");
            orchestrationWorker.AddOrchestration(typeof(CopyOrchestration<T>), CopyOrchestration<T>.Name, "1.0");
            orchestrationWorker.AddOrchestration(typeof(WaitDependsOnOrchestration<T>),WaitDependsOnOrchestration<T>.Name,"1.0");
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
            using (var db = new SQLServerAccess(this.options.Database.ConnectionString))
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
                   TraceHelper.TraceException(System.Diagnostics.TraceEventType.Critical, "ARMOrchestration-WaitDependsOnWorker", ex,$"WaitDependsOnWorker execute error:{ex.Message};{ex.StackTrace}");
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
            using var db = new SQLServerAccess(this.options.Database.ConnectionString);
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
            await Utility.ExecuteSqlScriptAsync("drop-schema.sql", this.options.Database);
        }

        public async Task CreateIfNotExistsAsync(bool recreate)
        {
            if (recreate) await DeleteARMOrchestrationTableAsync();
            var s = await Utility.GetScriptTextAsync("create-schema.sql");
            var s1 = string.Format(s, this.options.Database.SchemaName, this.options.Database.HubName, _Infrastructure.BuiltinServiceTypes.Deployments);
            await Utility.ExecuteSqlScriptAsync(s1, this.options.Database.ConnectionString);
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