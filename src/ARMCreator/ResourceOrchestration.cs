using DurableTask.Core;
using maskx.OrchestrationCreator.Activity;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator
{
    public class ResourceOrchestration : TaskOrchestration<string, ResourceOrchestrationInput>
    {
        private const string dependsOnEventName = "DependsOn";
        private TaskCompletionSource<string> dependsOnwaitHandler = null;

        public override async Task<string> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            if (null != input.Resource.Condition)
            {
                if (input.Resource.Condition is bool b)
                {
                    if (!b) return string.Empty;
                }
                else if (input.Resource.Condition is string s)
                {
                    var c = ARMFunctions.Evaluate(s, input.OrchestrationContext);
                    if (c is bool b1 && !b1)
                        return string.Empty;
                }
            }
            if (input.Resource.DependsOn.Count > 0)
            {
                dependsOnwaitHandler = new TaskCompletionSource<string>();
                await context.ScheduleTask<string>(typeof(SetDependsOnActivity), "");
                await dependsOnwaitHandler.Task;
            }

            return string.Empty;
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.dependsOnwaitHandler != null && name == dependsOnEventName)
            {
                this.dependsOnwaitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}