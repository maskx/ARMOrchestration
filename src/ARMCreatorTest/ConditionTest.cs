using DurableTask.Core;
using maskx.OrchestrationCreator;
using maskx.OrchestrationCreator.ARMTemplate;
using maskx.OrchestrationService;
using maskx.OrchestrationService.OrchestrationCreator;
using maskx.OrchestrationService.Worker;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ARMCreatorTest
{
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
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker, "Condition/TrueCondition");
        }

        [Fact(DisplayName = "EmptyCondition")]
        public void EmptyCondition()
        {
            ResourceOrchestrationInput input = new ResourceOrchestrationInput()
            {
            };
            ResourceOrchestration createOrUpdateOrchestration = new ResourceOrchestration();
        }

        [Fact(DisplayName = "NoCondition")]
        public void NoCondition()
        {
        }

        [Fact(DisplayName = "FunctionCondition")]
        public void FunctionCondition()
        {
            ResourceOrchestrationInput input = new ResourceOrchestrationInput()
            {
            };
            ResourceOrchestration createOrUpdateOrchestration = new ResourceOrchestration();
        }

        [Fact(DisplayName = "WrongFunctionCondition")]
        public void WrongFunctionCondition()
        {
            ResourceOrchestrationInput input = new ResourceOrchestrationInput()
            {
            };
            ResourceOrchestration createOrUpdateOrchestration = new ResourceOrchestration();
        }
    }
}