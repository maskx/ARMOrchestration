using maskx.ARMOrchestration;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ARMOrchestrationTest
{
    public class WhatIfTest
    {
        private ARMTemplateHelper templateHelper = new ARMTemplateHelper(
            Options.Create(new ARMOrchestrationOptions()
            {
                ExtensionResources = new List<string>()
            }),
            new ARMFunctions(Options.Create(new ARMOrchestrationOptions()
            {
                ListFunction = (sp, resourceId, apiVersion, functionValues, value) =>
                {
                    return new TaskResult() { };
                }
            }), null), null);

        [Fact]
        public void dd()
        {
        }
    }
}