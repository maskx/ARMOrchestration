using ARMCreatorTest;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.WhatIf;
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
            Options.Create(new ARMOrchestrationOptions
            {
                Database = new DatabaseConfig()
                {
                    ConnectionString = TestHelper.ConnectionString,
                    AutoCreate = true
                }
            }),
            new ARMFunctions(
                Options.Create(new ARMOrchestrationOptions()),
                null,
                new Mock.MockInfrastructure(null)),
            null,
            new Mock.MockInfrastructure(null));

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
                Template = TestHelper.GetTemplateContent("condition/truecondition")
            });
            Assert.NotNull(result);
        }
    }
}