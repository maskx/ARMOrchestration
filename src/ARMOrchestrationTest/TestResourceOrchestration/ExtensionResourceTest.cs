using ARMCreatorTest;
using maskx.OrchestrationService.Activity;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "ExtensionResource")]
    public class ExtensionResourceTest
    {
        private ARMOrchestartionFixture fixture;

        public ExtensionResourceTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "Run")]
        public void Run()
        {
            bool tagsAdded = false;
            var _ = new TraceActivityEventListener((args) =>
            {
                if (args.EventType.Equals("ExtensionResourceSuccessed"))
                {
                    tagsAdded = true;
                }
            });
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                            "ExtensionResource/tags");
            Assert.True(tagsAdded);
        }
    }
}