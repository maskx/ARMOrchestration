using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.PluginTests
{
    public class AfterResourceProvisioningOrchestation : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        public override  Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            return Task.FromResult( new TaskResult() { Code = 200, Content = DataConverter.Serialize(input) });
        }
    }
}