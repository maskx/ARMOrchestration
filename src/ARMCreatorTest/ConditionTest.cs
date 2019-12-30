using maskx.OrchestrationService;
using Xunit;

namespace ARMCreatorTest
{
    [Collection("WebHost ARMOrchestartion")]
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
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker, "Condition/TrueCondition", (instance, args) =>
             {
                 if (args.IsSubOrchestration && args.ParentExecutionId == instance.ExecutionId)
                 {
                     var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                     Assert.Equal(201, r.Code);
                 }
             });
        }

        [Fact(DisplayName = "NoCondition")]
        public void NoCondition()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker, "Condition/NoCondition", (instance, args) =>
            {
                if (args.IsSubOrchestration && args.ParentExecutionId == instance.ExecutionId)
                {
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                    Assert.Equal(201, r.Code);
                }
            });
        }

        [Fact(DisplayName = "FunctionConditionTrue")]
        public void FunctionConditionTrue()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker, "Condition/FunctionConditionTrue", (instance, args) =>
            {
                if (args.IsSubOrchestration && args.ParentExecutionId == instance.ExecutionId)
                {
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                    Assert.Equal(201, r.Code);
                }
            });
        }

        [Fact(DisplayName = "FunctionConditionFalse")]
        public void FunctionConditionFalse()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker, "Condition/FunctionConditionFalse", (instance, args) =>
            {
                if (args.IsSubOrchestration && args.ParentExecutionId == instance.ExecutionId)
                {
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                    Assert.Equal(200, r.Code);
                }
            });
        }
    }
}