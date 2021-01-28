using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using Microsoft.Extensions.DependencyInjection;
using System;
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
        private readonly IInfrastructure infrastructure;
        public ReTryTests(ARMOrchestartionFixture fixture)
        {
            this._Fixture = fixture;
            this.infrastructure = this._Fixture.ServiceProvider.GetService<IInfrastructure>();
            this._Client = this._Fixture.ServiceProvider.GetService<ARMOrchestrationClient<CustomCommunicationJob>>();
        }
        [Fact(DisplayName = "RetryCopy")]
        public async Task RetryCopy()
        {
            string correlationId = Guid.NewGuid().ToString("N");
            var instance = TestHelper.OrchestrationTest(_Fixture,
                  "CopyIndex/ResourceIteration_BatchSize", subscriptionId: Guid.NewGuid().ToString());
            DeploymentOperation op;
            string resId = string.Empty;
            string copyId = string.Empty;
            var rs = await _Client.GetAllResourceListAsync(instance.InstanceId);
            foreach (var r in rs)
            {
                if (r.Type == $"{infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}")
                    copyId = r.Id;
                if (r.Name == "0storage")
                    resId = r.Id;
            }
            #region not re-run
            await _Client.Retry(copyId, correlationId, "RetryUser1");
            do
            {
                await Task.Delay(5000);
                op = await _Client.GetDeploymentOperationAsync(copyId);
                if (op.Stage < 0)
                    break;
            } while (op.Stage != ProvisioningStage.Successed);
            Assert.Equal(ProvisioningStage.Successed, op.Stage);
            Assert.Equal(TestHelper.CreateByUserId, op.LastRunUserId);
            #endregion

            #region only run copy
            await TestHelper.ChangeOperationStage(copyId);
            await _Client.Retry(copyId, correlationId, "RetryUser2");
            do
            {
                await Task.Delay(5000);
                op = await _Client.GetDeploymentOperationAsync(copyId);
                if (op.Stage < 0)
                    break;
            } while (op.Stage != ProvisioningStage.Successed);
            Assert.Equal(ProvisioningStage.Successed, op.Stage);
            Assert.Equal("RetryUser2", op.LastRunUserId);
            rs = await _Client.GetAllResourceListAsync(instance.InstanceId);
            foreach (var r in rs)
            {
                if (r.Type == $"{infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}")
                    Assert.Equal("RetryUser2", r.LastRunUserId);
                else
                    Assert.Equal(TestHelper.CreateByUserId, r.LastRunUserId);
            }
            #endregion

            #region re-run copy and one resource
            await TestHelper.ChangeOperationStage(copyId);
            await TestHelper.ChangeOperationStage(resId);
            await _Client.Retry(copyId, correlationId, "RetryUser3");
            do
            {
                await Task.Delay(5000);
                op = await _Client.GetDeploymentOperationAsync(copyId);
                if (op.Stage < 0)
                    break;
            } while (op.Stage != ProvisioningStage.Successed);
            Assert.Equal(ProvisioningStage.Successed, op.Stage);
            Assert.Equal("RetryUser3", op.LastRunUserId);
            rs = await _Client.GetAllResourceListAsync(instance.InstanceId);
            foreach (var r in rs)
            {
                if (r.Type == $"{infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}")
                    Assert.Equal("RetryUser3", r.LastRunUserId);
                else if (r.Name == "0storage")
                    Assert.Equal("RetryUser3", r.LastRunUserId);
                else
                    Assert.Equal(TestHelper.CreateByUserId, r.LastRunUserId);
            }
            #endregion

        }
        [Fact(DisplayName = "RetryResource")]
        public async Task RetryResource()
        {
            var correlationId = Guid.NewGuid().ToString("N");
            var instance = TestHelper.OrchestrationTest(_Fixture,
                  "CopyIndex/ResourceIteration_BatchSize", subscriptionId: Guid.NewGuid().ToString());
            DeploymentOperation op;
            string opId = string.Empty;
            var rs = await _Client.GetAllResourceListAsync(instance.InstanceId);
            foreach (var r in rs)
            {
                if (r.Name == "0storage")
                    opId = r.Id;
            }
            #region not re-run
            await _Client.Retry(opId, correlationId, "RetryUser1");
            do
            {
                await Task.Delay(5000);
                op = await _Client.GetDeploymentOperationAsync(opId);
                if (op.Stage < 0)
                    break;
            } while (op.Stage != ProvisioningStage.Successed);
            Assert.Equal(ProvisioningStage.Successed, op.Stage);
            Assert.Equal(TestHelper.CreateByUserId, op.LastRunUserId);
            #endregion

            #region 
            await TestHelper.ChangeOperationStage(opId);
            await _Client.Retry(opId, correlationId, "RetryUser1");
            do
            {
                await Task.Delay(5000);
                op = await _Client.GetDeploymentOperationAsync(opId);
                if (op.Stage < 0)
                    break;
            } while (op.Stage != ProvisioningStage.Successed);
            Assert.Equal(ProvisioningStage.Successed, op.Stage);
            Assert.Equal("RetryUser1", op.LastRunUserId);
            #endregion
        }
        [Fact(DisplayName = "RetryDeployment")]
        public async Task RetryDeployment()
        {
            var correlationId = Guid.NewGuid().ToString("N");
            var instance = TestHelper.OrchestrationTest(_Fixture,
                  "CopyIndex/ResourceIteration_BatchSize", subscriptionId: Guid.NewGuid().ToString());

            #region Deployment success, will not re-run anyone
            await _Client.Retry(instance.InstanceId, correlationId, "RetryUser2");
            DeploymentOperation op;
            do
            {
                await Task.Delay(5000);
                op = await _Client.GetDeploymentOperationAsync(instance.InstanceId);
                if (op.Stage < 0)
                    break;
            } while (op.Stage != ProvisioningStage.Successed);
            Assert.Equal(ProvisioningStage.Successed, op.Stage);
            // retry will not run, because the original provisioning is successed. 
            Assert.Equal(TestHelper.CreateByUserId, op.LastRunUserId);
            #endregion

            #region Only re-run deployment
            await TestHelper.ChangeOperationStage(instance.InstanceId);
            await _Client.Retry(instance.InstanceId, correlationId, "Retry1");
            do
            {
                await Task.Delay(5000);
                op = await _Client.GetDeploymentOperationAsync(instance.InstanceId);
                if (op.Stage < 0)
                    break;
            } while (op.Stage != ProvisioningStage.Successed);
            Assert.Equal(ProvisioningStage.Successed, op.Stage);
            Assert.Equal("Retry1", op.LastRunUserId);

            var rs = await _Client.GetAllResourceListAsync(instance.InstanceId);
            // all child resource will not retry, because they all are successed.
            foreach (var r in rs)
            {
                if (r.Id == instance.InstanceId)
                    Assert.Equal("Retry1", r.LastRunUserId);
                else
                    Assert.Equal(TestHelper.CreateByUserId, r.LastRunUserId);
            }
            #endregion

            #region re-run deployment and copy
            await TestHelper.ChangeOperationStage(instance.InstanceId);
            foreach (var r in rs)
            {
                if (r.Type == $"{infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}")
                    await TestHelper.ChangeOperationStage(r.Id);
            }

            await _Client.Retry(instance.InstanceId, correlationId, "Retry2");
            do
            {
                await Task.Delay(5000);
                op = await _Client.GetDeploymentOperationAsync(instance.InstanceId);
                if (op.Stage < 0)
                    break;
            } while (op.Stage != ProvisioningStage.Successed);
            Assert.Equal(ProvisioningStage.Successed, op.Stage);
            Assert.Equal("Retry2", op.LastRunUserId);
            rs = await _Client.GetAllResourceListAsync(instance.InstanceId);
            foreach (var r in rs)
            {
                if (r.Id == instance.InstanceId)
                    Assert.Equal("Retry2", r.LastRunUserId);
                else if (r.Type == $"{infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}")
                    Assert.Equal("Retry2", r.LastRunUserId);
                else
                    Assert.Equal(TestHelper.CreateByUserId, r.LastRunUserId);
            }
            #endregion

            #region re-run deployment copy and one resource
            await TestHelper.ChangeOperationStage(instance.InstanceId);
            foreach (var r in rs)
            {
                if (r.Type == $"{infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}")
                    await TestHelper.ChangeOperationStage(r.Id);
                else if (r.Name == "0storage")
                    await TestHelper.ChangeOperationStage(r.Id);
            }

            await _Client.Retry(instance.InstanceId, correlationId, "Retry3");
            do
            {
                await Task.Delay(5000);
                op = await _Client.GetDeploymentOperationAsync(instance.InstanceId);
                if (op.Stage < 0)
                    break;
            } while (op.Stage != ProvisioningStage.Successed);
            Assert.Equal(ProvisioningStage.Successed, op.Stage);
            Assert.Equal("Retry3", op.LastRunUserId);
            rs = await _Client.GetAllResourceListAsync(instance.InstanceId);
            foreach (var r in rs)
            {
                if (r.Id == instance.InstanceId)
                    Assert.Equal("Retry3", r.LastRunUserId);
                else if (r.Type == $"{infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}")
                    Assert.Equal("Retry3", r.LastRunUserId);
                else if (r.Name == "0storage")
                    Assert.Equal("Retry3", r.LastRunUserId);
                else
                    Assert.Equal(TestHelper.CreateByUserId, r.LastRunUserId);
            }
            #endregion
        }
        [Fact(DisplayName = "SameUser_SameCorrelationId")]
        public async Task SameUser_SameCorrelationId()
        {
            var correlationId = Guid.NewGuid().ToString("N");
            var instance = TestHelper.OrchestrationTest(_Fixture,
                "CopyIndex/ResourceIteration_BatchSize", subscriptionId: Guid.NewGuid().ToString());

            #region SameUser SameCorrelationId
            await TestHelper.ChangeOperationStage(instance.InstanceId);
            var op = await _Client.GetDeploymentOperationAsync(instance.InstanceId);
            var r1 = await _Client.RetryDeployment(op, correlationId, "Retry1");
            var r2 = await _Client.RetryDeployment(op, correlationId, "Retry1");
            Assert.Equal(201, r1.Result);
            Assert.Equal(202, r2.Result);
            #endregion


        }
        [Fact(DisplayName = "SameUser_DifferentCorrelationId")]
        public async Task SameUser_DifferentCorrelationId()
        {
            var correlationId = Guid.NewGuid().ToString("N");
            var instance = TestHelper.OrchestrationTest(_Fixture,
                "CopyIndex/ResourceIteration_BatchSize", subscriptionId: Guid.NewGuid().ToString());

            #region Same UserId ,different correlationId
            await TestHelper.ChangeOperationStage(instance.InstanceId);
            var op = await _Client.GetDeploymentOperationAsync(instance.InstanceId);
            var r1 = await _Client.RetryDeployment(op, correlationId, "Retry1");
            var r2 = await _Client.RetryDeployment(op, Guid.NewGuid().ToString("N"), "Retry1");
            Assert.Equal(201, r1.Result);
            Assert.Equal(400, r2.Result);
            #endregion

        }
    }
}
