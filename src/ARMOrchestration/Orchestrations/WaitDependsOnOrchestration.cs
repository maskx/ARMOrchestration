using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activity
{
    public class WaitDependsOnOrchestration : TaskOrchestration<TaskResult, (string dependsOn, Dictionary<string, object> context)>
    {
        private const string commandTemplate = "insert into {0} () values()";
        private readonly string commandText;
        private const string eventName = "WaitDependsOn";
        private TaskCompletionSource<string> waitHandler = null;
        private TemplateOrchestrationOptions options;

        public WaitDependsOnOrchestration(IOptions<TemplateOrchestrationOptions> options)
        {
            this.options = options?.Value;
            this.commandText = string.Format(commandTemplate, this.options.Database.WaitDependsOnTableName);
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, (string dependsOn, Dictionary<string, object> context) input)
        {
            using var jsonDoc = JsonDocument.Parse(input.dependsOn);
            Dictionary<string, object> pars = new Dictionary<string, object>();
            foreach (var item in jsonDoc.RootElement.EnumerateArray())
            {
            }
            waitHandler = new TaskCompletionSource<string>();
            using (var db = new DbAccess(this.options.Database.ConnectionString))
            {
                db.AddStatement(this.commandText, pars);
                await db.ExecuteNonQueryAsync();
            }
            await waitHandler.Task;
            return new TaskResult();
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.waitHandler != null && name == eventName)
            {
                this.waitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}