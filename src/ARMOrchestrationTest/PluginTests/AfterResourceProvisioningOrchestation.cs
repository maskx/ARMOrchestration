using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using System;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.PluginTests
{
    public class AfterResourceProvisioningOrchestation : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            return new TaskResult() { Code = 200, Content = DataConverter.Serialize(input) };
        }
    }
}