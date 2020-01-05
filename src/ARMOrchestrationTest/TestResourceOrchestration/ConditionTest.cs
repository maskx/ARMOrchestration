using maskx.OrchestrationService;
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
                "Condition/TrueCondition",
                (instance, args) =>
                {
                    return args.IsSubOrchestration && args.ParentExecutionId == instance.ExecutionId;
                },
                (instance, args) =>
                {
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                    Assert.Equal(200, r.Code);
                });
        }

        [Fact(DisplayName = "NoCondition")]
        public void NoCondition()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "Condition/NoCondition",
                (instance, args) =>
                {
                    return args.IsSubOrchestration && args.ParentExecutionId == instance.ExecutionId;
                },
                (instance, args) =>
                {
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                    Assert.Equal(200, r.Code);
                });
        }

        [Fact(DisplayName = "FunctionConditionTrue")]
        public void FunctionConditionTrue()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "Condition/FunctionConditionTrue",
                (instance, args) =>
                {
                    return args.IsSubOrchestration && args.ParentExecutionId == instance.ExecutionId;
                },
                (instance, args) =>
                {
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                    Assert.Equal(200, r.Code);
                });
        }

        [Fact(DisplayName = "FunctionConditionFalse")]
        public void FunctionConditionFalse()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "Condition/FunctionConditionFalse",
                (instance, args) =>
                {
                    return args.IsSubOrchestration
                    && args.ParentExecutionId == instance.ExecutionId
                    && args.Id == "0";
                },
                (instance, args) =>
                {
                    var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                    Assert.Equal(200, r.Code);
                    Assert.Equal("condition is false", r.Content);
                });
        }
    }
}