using DurableTask.Core;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ARMOrchestrationTest.DeploymentTests
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "DeploymentTests")]
    public class TemplateLinkTest
    {
        private readonly ARMOrchestartionFixture fixture;

        public TemplateLinkTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }
        [Fact(DisplayName = "NestTemplateLink")]
        public void NestTemplateLink()
        {
            TestHelper.OrchestrationTest(fixture, "DeploymentTests/json/NestTemplateLink_main", null,null,true);
        }
    }
}
