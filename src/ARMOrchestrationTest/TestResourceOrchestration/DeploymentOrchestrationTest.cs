using DurableTask.Core;
using maskx.OrchestrationService;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "DeploymentOrchestration")]
    public class DeploymentOrchestrationTest
    {
        private readonly ARMOrchestartionFixture fixture;

        public DeploymentOrchestrationTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "HasResourceFail")]
        public void HasResourceFail()
        {
            var (instance, result) = TestHelper.OrchestrationTestNotCheckResult(this.fixture, "HasResourceFail",TestHelper.SubscriptionId);
            Assert.Equal(OrchestrationStatus.Completed, result.OrchestrationStatus);
            var response = TestHelper.DataConverter.Deserialize<TaskResult>(result.Output);
            Assert.Equal(500, response.Code);
            var rs = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.ExecutionId).Result;
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