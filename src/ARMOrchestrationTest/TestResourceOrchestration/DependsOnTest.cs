using ARMCreatorTest;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "DependsOn")]
    public class DependsOnTest
    {
        private readonly ARMOrchestartionFixture fixture;

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

        [Fact(DisplayName = "CopyLoop")]
        public void CopyLoop()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "dependsOn/copyloop");
        }

        [Fact(DisplayName = "ConditionFalse")]
        public void ConditionFalse()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "dependsOn/ConditionFalse");
        }
    }
}