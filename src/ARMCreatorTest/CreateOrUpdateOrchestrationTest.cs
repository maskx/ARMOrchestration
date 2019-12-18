using DurableTask.Core;
using maskx.OrchestrationCreator;
using System;
using Xunit;

namespace ARMCreatorTest
{
    [Trait("c", "CreateOrUpdateOrchestration")]
    public class CreateOrUpdateOrchestrationTest
    {
        [Fact(DisplayName = "TrueCondition")]
        public void TrueCondition()
        {
            CreateOrUpdateInput input = new CreateOrUpdateInput()
            {
                Resource = "{\"condition\": true}"
            };
            CreateOrUpdateOrchestration createOrUpdateOrchestration = new CreateOrUpdateOrchestration();

            createOrUpdateOrchestration.RunTask(null, input).Wait();
        }

        [Fact(DisplayName = "EmptyCondition")]
        public void EmptyCondition()
        {
            CreateOrUpdateInput input = new CreateOrUpdateInput()
            {
                Resource = "{\"condition\": \"\"}"
            };
            CreateOrUpdateOrchestration createOrUpdateOrchestration = new CreateOrUpdateOrchestration();

            createOrUpdateOrchestration.RunTask(null, input).Wait();
        }

        [Fact(DisplayName = "NoCondition")]
        public void NoCondition()
        {
            CreateOrUpdateInput input = new CreateOrUpdateInput()
            {
                Resource = "{}"
            };
            CreateOrUpdateOrchestration createOrUpdateOrchestration = new CreateOrUpdateOrchestration();

            createOrUpdateOrchestration.RunTask(null, input).Wait();
        }

        [Fact(DisplayName = "FunctionCondition")]
        public void FunctionCondition()
        {
            CreateOrUpdateInput input = new CreateOrUpdateInput()
            {
                Resource = "{\"condition\": \"[bool(1)]\"}"
            };
            CreateOrUpdateOrchestration createOrUpdateOrchestration = new CreateOrUpdateOrchestration();

            createOrUpdateOrchestration.RunTask(null, input).Wait();
        }

        [Fact(DisplayName = "WrongFunctionCondition")]
        public void WrongFunctionCondition()
        {
            CreateOrUpdateInput input = new CreateOrUpdateInput()
            {
                Resource = "{\"condition\": \"[[abc]\"}"
            };
            CreateOrUpdateOrchestration createOrUpdateOrchestration = new CreateOrUpdateOrchestration();

            createOrUpdateOrchestration.RunTask(null, input).Wait();
        }
    }
}