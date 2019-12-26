using DurableTask.Core;
using maskx.OrchestrationCreator;
using maskx.OrchestrationCreator.ARMTemplate;
using maskx.OrchestrationService;
using maskx.OrchestrationService.OrchestrationCreator;
using maskx.OrchestrationService.Worker;
using System;
using Xunit;

namespace ARMCreatorTest
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "CreateOrUpdateOrchestration")]
    public class CreateOrUpdateOrchestrationTest
    {
        private ARMOrchestartionFixture fixture;

        public CreateOrUpdateOrchestrationTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }
    }
}