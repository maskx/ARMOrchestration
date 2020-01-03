using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activities
{
    public class SaveDeploymentResultActivity : TaskActivity<TaskResult, TaskResult>
    {
        private readonly TemplateOrchestrationOptions options;
        private readonly string commandText;

        public SaveDeploymentResultActivity(IOptions<TemplateOrchestrationOptions> options)
        {
            this.options = options.Value;
        }

        protected override TaskResult Execute(TaskContext context, TaskResult input)
        {
            return ExecuteAsync(context, input).Result;
        }

        protected override async Task<TaskResult> ExecuteAsync(TaskContext context, TaskResult input)
        {
            Dictionary<string, object> pars = new Dictionary<string, object>();
            using (var db = new DbAccess(this.options.ConnectionString))
            {
                db.AddStatement(this.commandText, pars);
                await db.ExecuteNonQueryAsync();
            }
            return new TaskResult() { Code = 200 };
        }
    }
}