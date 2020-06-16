using Xunit;

namespace ARMCreatorTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "Condition")]
    public class ConditionTest
    {
        private ARMOrchestartionFixture fixture;

        public ConditionTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "TrueCondition")]
        public void TrueCondition()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "Condition/TrueCondition");
        }

        [Fact(DisplayName = "NoCondition")]
        public void NoCondition()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "Condition/NoCondition");
        }

        [Fact(DisplayName = "FunctionConditionTrue")]
        public void FunctionConditionTrue()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "Condition/FunctionConditionTrue");
        }

        [Fact(DisplayName = "FunctionConditionFalse")]
        public void FunctionConditionFalse()
        {
            var instance = TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                       "Condition/FunctionConditionFalse");
            var r = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            Assert.Single(r);
        }
    }
}