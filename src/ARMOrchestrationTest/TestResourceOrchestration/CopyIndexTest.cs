using ARMCreatorTest;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "CopyIndex")]
    public class CopyIndexTest
    {
        private ARMOrchestartionFixture fixture;

        public CopyIndexTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "ResourceIteration")]
        public void ResourceIteration()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "CopyIndex/ResourceIteration");
        }
    }
}