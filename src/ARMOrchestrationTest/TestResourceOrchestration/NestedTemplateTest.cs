using ARMCreatorTest;
using maskx.OrchestrationService;
using System.Text.Json;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "NestedTemplate")]
    public class NestedTemplateTest
    {
        private ARMOrchestartionFixture fixture;

        public NestedTemplateTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "NestTemplate")]
        public void NestTemplate()
        {
            TestHelper.OrchestrationTest(fixture,
               "NestTemplate/NestTemplate");
        }

        [Fact(DisplayName = "ExpressionsInNestedTemplates-inner")]
        public void ExpressionsInNestedTemplates_inner()
        {
            TestHelper.OrchestrationTest(fixture,
              "NestTemplate/ExpressionsInNestedTemplates-inner",
              (instance, args) =>
              {
                  return !args.IsSubOrchestration && args.InstanceId == instance.InstanceId;
              }, (instance, args) =>
              {
                  Assert.True(args.Status);
                  var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                  Assert.Equal(200, r.Code);
                  using var doc = JsonDocument.Parse(r.Content);
                  var root = doc.RootElement;
                  Assert.True(root.GetProperty("properties").GetProperty("outputs").TryGetProperty("messageFromLinkedTemplate", out JsonElement messageFromLinkedTemplate));

                  Assert.Equal("from nested template", messageFromLinkedTemplate.GetProperty("value").GetString());
              });
        }
    }
}