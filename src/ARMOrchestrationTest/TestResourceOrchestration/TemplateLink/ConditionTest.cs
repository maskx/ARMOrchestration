using System;
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
                "Templates/Condition/TrueCondition", subscriptionId: Guid.NewGuid().ToString(), isValidateOrchestration: null, validate: null, usingLinkTemplate: true);
        }

        [Fact(DisplayName = "NoCondition")]
        public void NoCondition()
        {
            TestHelper.OrchestrationTest(fixture,
                "Templates/Condition/NoCondition", subscriptionId: Guid.NewGuid().ToString(), isValidateOrchestration: null, validate: null, usingLinkTemplate: true);
        }

        [Fact(DisplayName = "FunctionConditionTrue")]
        public void FunctionConditionTrue()
        {
            TestHelper.OrchestrationTest(fixture,
                "Templates/Condition/FunctionConditionTrue", subscriptionId: Guid.NewGuid().ToString(), isValidateOrchestration: null, validate: null, usingLinkTemplate: true);
        }

        [Fact(DisplayName = "FunctionConditionFalse")]
        public void FunctionConditionFalse()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                       "Templates/Condition/FunctionConditionFalse", subscriptionId: Guid.NewGuid().ToString(), isValidateOrchestration: null, validate: null, usingLinkTemplate: true);
            var r = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            Assert.Single(r);
        }
    }
}