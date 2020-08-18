using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using System;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.PluginTests
{
    public class BeforeResourceProvisioningOrchestation : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        private readonly IServiceProvider _ServiceProvider;

        public BeforeResourceProvisioningOrchestation(IServiceProvider serviceProvider)
        {
            this._ServiceProvider = serviceProvider;
        }

        public override Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            input.ServiceProvider = _ServiceProvider;
            input.Resource.Name += "_BeforeResourceProvisioning";
            return Task.FromResult(new TaskResult() { Code = 200, Content = DataConverter.Serialize(input) });
        }
    }
}