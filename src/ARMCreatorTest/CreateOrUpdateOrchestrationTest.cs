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