using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration.TemplateLink
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "TemplateLink_Condition")]
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
                "Templates/Condition/TrueCondition", null,null,true);
        }

        [Fact(DisplayName = "NoCondition")]
        public void NoCondition()
        {
            TestHelper.OrchestrationTest(fixture,
                "Templates/Condition/NoCondition", null, null, true);
        }

        [Fact(DisplayName = "FunctionConditionTrue")]
        public void FunctionConditionTrue()
        {
            TestHelper.OrchestrationTest(fixture,
                "Templates/Condition/FunctionConditionTrue", null, null, true);
        }

        [Fact(DisplayName = "FunctionConditionFalse")]
        public void FunctionConditionFalse()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                       "Templates/Condition/FunctionConditionFalse", null, null, true);
            var r = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            Assert.Single(r);
        }
    }
}