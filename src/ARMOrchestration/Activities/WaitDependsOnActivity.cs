using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class WaitDependsOnActivity : TaskActivity<(string DeploymentId, string DependsOn, Dictionary<string, object> Context), TaskResult>
    {
        private const string commandTemplate = @"
insert into {0}
(DeploymentId,InstanceId,ExecutionId,DependsOnName,CreateTime)
values
(@DeploymentId,@InstanceId,@ExecutionId,@DependsOnName,GETUTCDATE())";

        private readonly string commandText;
        private TemplateOrchestrationOptions options;

        public WaitDependsOnActivity(IOptions<TemplateOrchestrationOptions> options)
        {
            this.options = options?.Value;
            this.commandText = string.Format(commandTemplate, this.options.Database.WaitDependsOnTableName);
        }

        protected override TaskResult Execute(TaskContext context, (string DeploymentId, string DependsOn, Dictionary<string, object> Context) input)
        {
            return ExecuteAsync(context, input).Result;
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, (string DeploymentId, string DependsOn, Dictionary<string, object> Context) input)
        {
            using var jsonDoc = JsonDocument.Parse(input.DependsOn);
            using (var db = new DbAccess(this.options.Database.ConnectionString))
            {
                foreach (var item in jsonDoc.RootElement.EnumerateArray())
                {
                    Dictionary<string, object> pars = new Dictionary<string, object>()
                    {
                        {"DeploymentId",input.DeploymentId },
                        {"InstanceId",context.OrchestrationInstance.InstanceId },
                        { "ExecutionId",context.OrchestrationInstance.ExecutionId},
                        { "DependsOnName",ARMFunctions.Evaluate(item.GetString(),input.Context).ToString()}
                    };
                    db.AddStatement(this.commandText, pars);
                }
                await db.ExecuteNonQueryAsync();
            }
            return new TaskResult() { Code = 200 };
        }
    }
}