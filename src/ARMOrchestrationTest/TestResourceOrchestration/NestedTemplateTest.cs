﻿using ARMCreatorTest;
using maskx.OrchestrationService;
using System;
using System.Collections.Generic;
using System.Text;
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
            fixture.ARMFunctions.SetFunction("reference", (args, cxt) =>
            {
                args.Result = "1231";
            });
        }

        [Fact(DisplayName = "NestTemplate")]
        public void NestTemplate()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
               "NestTemplate/NestTemplate");
        }

        [Fact(DisplayName = "ExpressionsInNestedTemplates-inner")]
        public void ExpressionsInNestedTemplates_inner()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
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
                  Assert.True(root.TryGetProperty("messageFromLinkedTemplate", out JsonElement messageFromLinkedTemplate));

                  //  Assert.Equal("from nested template", messageFromLinkedTemplate.GetProperty("value").GetString());
              });
        }
    }
}