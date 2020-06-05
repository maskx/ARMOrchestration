using ARMCreatorTest;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("ResourceOrchestration", "DeploymentOrchestration")]
    public class DeploymentOrchestrationTest
    {
        private ARMOrchestartionFixture fixture;

        public DeploymentOrchestrationTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "HasResourceFail")]
        public void HasResourceFail()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
            };
            var instance = TestHelper.OrchestrationTest(this.fixture.OrchestrationWorker, "HasResourceFail");
            var rs = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            foreach (var r in rs)
            {
                if (r.Name == "fail")
                    Assert.Equal(maskx.ARMOrchestration.Activities.ProvisioningStage.ProvisioningResourceFailed, r.Stage);
                else if (r.Type == "Microsoft.Resources/deployments")
                    Assert.Equal(maskx.ARMOrchestration.Activities.ProvisioningStage.Failed, r.Stage);
                else
                    Assert.Equal(maskx.ARMOrchestration.Activities.ProvisioningStage.Successed, r.Stage);
            }
        }
    }
}