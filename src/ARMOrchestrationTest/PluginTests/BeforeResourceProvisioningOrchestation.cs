using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.PluginTests
{
    public class BeforeResourceProvisioningOrchestation : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        public override Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            // todo: need find method to test this
            //input.Resource.Name += "_BeforeResourceProvisioning";
            return Task.FromResult(new TaskResult() { Code = 200, Content = DataConverter.Serialize(input) });
        }
    }
}