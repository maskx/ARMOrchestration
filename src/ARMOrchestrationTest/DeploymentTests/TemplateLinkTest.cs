using System;
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
            TestHelper.OrchestrationTest(fixture, "DeploymentTests/json/NestTemplateLink_main", subscriptionId:Guid.NewGuid().ToString(),isValidateOrchestration: null, validate: null, usingLinkTemplate: true);
        }
    }
}
