using DurableTask.Core;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using System;
using System.Threading.Tasks;

namespace ARMOrchestrationTest.PluginTests
{
    public class AfterResourceProvisioningOrchestation : TaskOrchestration<TaskResult, ResInput>
    {
        public readonly IServiceProvider _ServiceProvider;

        public AfterResourceProvisioningOrchestation(IServiceProvider serviceProvider)
        {
            this._ServiceProvider = serviceProvider;
        }

        public override Task<TaskResult> RunTask(OrchestrationContext context, ResInput input)
        {
            input.ServiceProvider = _ServiceProvider;
            return Task.FromResult(new TaskResult() { Code = 200, Content = input });
        }
    }
}