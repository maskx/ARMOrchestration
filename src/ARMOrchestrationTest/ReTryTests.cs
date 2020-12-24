using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Activities;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace ARMOrchestrationTest
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "Retry")]
    public class ReTryTests
    {
        private readonly ARMOrchestartionFixture _Fixture;
        private readonly ARMOrchestrationClient<CustomCommunicationJob> _Client;
        public ReTryTests(ARMOrchestartionFixture fixture)
        {
            this._Fixture = fixture;
            this._Client = this._Fixture.ServiceProvider.GetService<ARMOrchestrationClient<CustomCommunicationJob>>();
        }
        [Fact(DisplayName = "RetryResource")]
        public async Task RetryResource()
        {
            //var instance = TestHelper.OrchestrationTest(_Fixture,
            //      "CopyIndex/ResourceIteration_BatchSize", subscriptionId: Guid.NewGuid().ToString());
            // await _Client.RetryResource("6234eb2914ba482a94a84ca63513e8dd", "1.0", "RetryUser1");
            // wait resourceOrchestration update the stage
            await Task.Delay(100000000);

            DeploymentOperation op;
            do
            {
                await Task.Delay(5000);
                op = await _Client.GetDeploymentOperationAsync("1200ca5a8fe84d8888f4d75c7a03a7e3");
                //if (op.Stage < 0)
                //    break;
            } while (op.Stage != ProvisioningStage.Successed);
            Assert.Equal(ProvisioningStage.Successed, op.Stage);
            Assert.Equal("RetryUser1", op.LastRunUserId);
        }
        [Fact(DisplayName = "RetryDeployment")]
        public async Task RetryDeployment()
        {
            //var instance = TestHelper.OrchestrationTest(_Fixture,
            //      "CopyIndex/ResourceIteration_BatchSize", subscriptionId: Guid.NewGuid().ToString());
            //await _Client.RetryDeployment("6234eb2914ba482a94a84ca63513e8dd", "1.0", "RetryUser2");
            //DeploymentOperation op;
            //do
            //{
            //    op = await _Client.GetDeploymentOperationAsync("1200ca5a8fe84d8888f4d75c7a03a7e3");
            //    if (op.Stage < 0)
            //        break;
            //} while (op.Stage != ProvisioningStage.Successed);
            //Assert.Equal(ProvisioningStage.Successed, op.Stage);
            //Assert.Equal("RetryUser2", op.LastRunUserId);
        }
    }
}
