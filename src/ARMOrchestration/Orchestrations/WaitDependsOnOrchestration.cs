using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class WaitDependsOnOrchestration : TaskOrchestration<TaskResult, (string DeploymentId, List<string> DependsOn)>
    {
        internal const string eventName = "WaitDependsOn";
        private TaskCompletionSource<string> waitHandler = null;

        public override async Task<TaskResult> RunTask(OrchestrationContext context, (string DeploymentId, List<string> DependsOn) input)
        {
            waitHandler = new TaskCompletionSource<string>();
            await context.ScheduleTask<TaskResult>(typeof(WaitDependsOnActivity).Name, "1.0", input);
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