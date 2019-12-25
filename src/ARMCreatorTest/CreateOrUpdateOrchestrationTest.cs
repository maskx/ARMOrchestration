using DurableTask.Core;
using maskx.OrchestrationCreator;
using maskx.OrchestrationCreator.ARMTemplate;
using maskx.OrchestrationService;
using maskx.OrchestrationService.OrchestrationCreator;
using maskx.OrchestrationService.Worker;
using System;
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

        [Trait("CreateOrUpdateOrchestration", "Condition")]
        [Fact(DisplayName = "TrueCondition")]
        public void TrueCondition()
        {
            var instance = new OrchestrationInstance() { InstanceId = Guid.NewGuid().ToString("N") };

            fixture.OrchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = instance.InstanceId,
                Orchestration = new Orchestration()
                {
                    Creator = "DICreator",
                    Uri = typeof(ARMOrchestration).FullName + "_"
                },
                Input = TestHelper.DataConverter.Serialize(new ARMOrchestrationInput()
                {
                    Template = Template.Parse(TestHelper.GetTemplateContent("Condition/TrueCondition")),
                    Parameters = string.Empty
                })
            }).Wait();
            while (true)
            {
                var result = TestHelper.TaskHubClient.WaitForOrchestrationAsync(instance, TimeSpan.FromSeconds(30)).Result;
                if (result != null)
                {
                    Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
                    var response = TestHelper.DataConverter.Deserialize<TaskResult>(result.Output);
                    Assert.Equal(200, response.Code);

                    break;
                }
            }
        }

        [Fact(DisplayName = "EmptyCondition")]
        public void EmptyCondition()
        {
            CreateOrUpdateInput input = new CreateOrUpdateInput()
            {
            };
            CreateOrUpdateOrchestration createOrUpdateOrchestration = new CreateOrUpdateOrchestration();
        }

        [Fact(DisplayName = "NoCondition")]
        public void NoCondition()
        {
        }

        [Fact(DisplayName = "FunctionCondition")]
        public void FunctionCondition()
        {
            CreateOrUpdateInput input = new CreateOrUpdateInput()
            {
            };
            CreateOrUpdateOrchestration createOrUpdateOrchestration = new CreateOrUpdateOrchestration();
        }

        [Fact(DisplayName = "WrongFunctionCondition")]
        public void WrongFunctionCondition()
        {
            CreateOrUpdateInput input = new CreateOrUpdateInput()
            {
            };
            CreateOrUpdateOrchestration createOrUpdateOrchestration = new CreateOrUpdateOrchestration();
        }
    }
}