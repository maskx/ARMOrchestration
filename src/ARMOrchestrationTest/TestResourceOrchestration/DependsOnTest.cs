using ARMCreatorTest;
using maskx.OrchestrationService;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "DependsOn")]
    public class DependsOnTest
    {
        private ARMOrchestartionFixture fixture;

        public DependsOnTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "OneResourceName")]
        public void OneResourceName()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "dependsOn/OneResourceName");
        }

        [Fact(DisplayName = "ThreeResourceName")]
        public void ThreeResourceName()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "dependsOn/ThreeResourceName");
        }
    }
}