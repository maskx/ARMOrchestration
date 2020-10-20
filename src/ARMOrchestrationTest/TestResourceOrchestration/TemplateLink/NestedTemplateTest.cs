using maskx.OrchestrationService;
using System;
using System.Text.Json;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration.TemplateLink
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "TemplateLink_NestedTemplate")]
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
               "Templates/NestTemplate/NestTemplate", subscriptionId: Guid.NewGuid().ToString(), isValidateOrchestration: null, validate: null, usingLinkTemplate: true);
        }

        [Fact(DisplayName = "ExpressionsInNestedTemplates-inner")]
        public void ExpressionsInNestedTemplates_inner()
        {
            TestHelper.OrchestrationTest(fixture,
              "Templates/NestTemplate/ExpressionsInNestedTemplates-inner", subscriptionId: Guid.NewGuid().ToString(),
              isValidateOrchestration: (instance, args) =>
 {
     return !args.IsSubOrchestration && args.InstanceId == instance.InstanceId;
 }, validate: (instance, args) =>
               {
                   Assert.True(args.Status);
                   var r = TestHelper.DataConverter.Deserialize<TaskResult>(args.Result);
                   Assert.Equal(200, r.Code);
                   using var doc = JsonDocument.Parse(r.Content.ToString());
                   var root = doc.RootElement;
                   Assert.True(root.GetProperty("properties").GetProperty("outputs").TryGetProperty("messageFromLinkedTemplate", out JsonElement messageFromLinkedTemplate));

                   Assert.Equal("from nested template", messageFromLinkedTemplate.GetProperty("value").GetString());
               }, usingLinkTemplate: true);
        }
    }
}