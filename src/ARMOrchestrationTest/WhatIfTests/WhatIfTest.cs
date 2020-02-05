using ARMCreatorTest;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ARMOrchestrationTest.WhatIfTests
{
    [Trait("c", "WhatIf")]
    public class WhatIfTest
    {
        private readonly string resource1Id = "";

        private ARMTemplateHelper templateHelper = new ARMTemplateHelper(
            Options.Create(new ARMOrchestrationOptions()
            {
                ExtensionResources = new List<string>()
            }),
            new ARMFunctions(Options.Create(new ARMOrchestrationOptions()
            {
                ListFunction = (sp, cxt, resourceId, apiVersion, functionValues, value) =>
                {
                    JObject obj = new JObject();
                    JArray values = new JArray();
                    JObject res1 = new JObject();
                    res1.Add("id", resourceId);
                    values.Add(res1);
                    obj.Add("values", values);
                    return new TaskResult()
                    {
                        Code = 200,
                        Content = obj.ToString()
                    };
                }
            }), null), null);

        [Fact(DisplayName = "WhatIfIncremental")]
        public void WhatIfIncremental()
        {
            var result = templateHelper.WhatIf(new PredictTemplateOrchestrationInput()
            {
                CorrelationId = Guid.NewGuid().ToString("N"),
                DeploymentName = "WhatIfTest",
                Mode = DeploymentMode.Incremental,
                ResourceGroupName = TestHelper.ResourceGroup,
                SubscriptionId = TestHelper.SubscriptionId,
                ScopeType = maskx.ARMOrchestration.WhatIf.ScopeType.ResourceGroup,
                ResultFormat = maskx.ARMOrchestration.WhatIf.WhatIfResultFormat.ResourceIdOnly,
                Template = TestHelper.GetTemplateContent("templates/condition/truecondition")
            });
            Assert.NotNull(result);
        }
    }
}