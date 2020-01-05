using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.DurableTask.SQLServer.SQL;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Activity
{
    public class WaitDependsOnOrchestration : TaskOrchestration<TaskResult, (string DeploymentId, string DependsOn, Dictionary<string, object> Context)>
    {
        internal const string eventName = "WaitDependsOn";
        private TaskCompletionSource<string> waitHandler = null;

        public override async Task<TaskResult> RunTask(OrchestrationContext context, (string DeploymentId, string DependsOn, Dictionary<string, object> Context) input)
        {
            waitHandler = new TaskCompletionSource<string>();
            await context.ScheduleTask<TaskResult>(typeof(WaitDependsOnActivity), input);
            await waitHandler.Task;
            return new TaskResult() { Code = 200 };
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.waitHandler != null && name == eventName && this.waitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.waitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}