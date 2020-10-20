using System;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "Condition")]
    public class ConditionTest
    {
        private readonly ARMOrchestartionFixture fixture;

        public ConditionTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "TrueCondition")]
        public void TrueCondition()
        {
            TestHelper.OrchestrationTest(fixture,
                "Condition/TrueCondition", subscriptionId: Guid.NewGuid().ToString());
        }

        [Fact(DisplayName = "NoCondition")]
        public void NoCondition()
        {
            TestHelper.OrchestrationTest(fixture,
                "Condition/NoCondition", subscriptionId: Guid.NewGuid().ToString());
        }

        [Fact(DisplayName = "FunctionConditionTrue")]
        public void FunctionConditionTrue()
        {
            TestHelper.OrchestrationTest(fixture,
                "Condition/FunctionConditionTrue", subscriptionId: Guid.NewGuid().ToString());
        }

        [Fact(DisplayName = "FunctionConditionFalse")]
        public void FunctionConditionFalse()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                       "Condition/FunctionConditionFalse", subscriptionId: Guid.NewGuid().ToString());
            var r = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            Assert.Single(r);
        }
    }
}