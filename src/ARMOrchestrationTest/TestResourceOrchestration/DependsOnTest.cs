using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "DependsOn")]
    public class DependsOnTest
    {
        private readonly ARMOrchestartionFixture fixture;

        public DependsOnTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "OneResourceName")]
        public void OneResourceName()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/OneResourceName", subscriptionId: Guid.NewGuid().ToString());
        }

        [Fact(DisplayName = "ThreeResourceName")]
        public void ThreeResourceName()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/ThreeResourceName", subscriptionId: Guid.NewGuid().ToString());
        }

        [Fact(DisplayName = "CopyLoop")]
        public void CopyLoop()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/copyloop", subscriptionId: Guid.NewGuid().ToString());
        }

        [Fact(DisplayName = "ConditionFalse")]
        public void ConditionFalse()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/ConditionFalse", subscriptionId: Guid.NewGuid().ToString());
        }

        [Fact(DisplayName = "DeployDepondsOn")]
        public void DeployDepondsOn()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/DeployDepondsOn", subscriptionId: Guid.NewGuid().ToString());
        }

        [Fact(DisplayName = "DuplicatedName")]
        public void DuplicatedName()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/DuplicatedName", subscriptionId: Guid.NewGuid().ToString());
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient<CustomCommunicationJob>>();
            var resources = client.GetDeploymentOperationListAsync(instance.InstanceId).Result;
            foreach (var r in resources)
            {
                if (r.Name == "resource2")
                {
                    var res = TestHelper.DataConverter.Deserialize<Resource>(r.Input);
                   
                    Assert.Equal(1,res.DependsOn.Count);
                    Assert.Equal("ns.rp/st/resource1", res.DependsOn[0]);
                }
            }
        }

        [Fact(DisplayName = "NameAndServiceTypeName")]
        public void NameAndServiceTypeName()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/NameAndServiceTypeName", subscriptionId: Guid.NewGuid().ToString());
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient<CustomCommunicationJob>>();
            var resources = client.GetDeploymentOperationListAsync(instance.InstanceId).Result;
            foreach (var r in resources)
            {
                if (r.Name == "resource2")
                {
                    var res = TestHelper.DataConverter.Deserialize<Resource>(r.Input);
                   
                    Assert.Equal(1, res.DependsOn.Count);
                    Assert.Equal("ns.rp/st/resource1", res.DependsOn[0]);
                }
            }
        }

        [Fact(DisplayName = "ServiceTypeNameAndName")]
        public void ServiceTypeNameAndName()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/ServiceTypeNameAndName", subscriptionId: Guid.NewGuid().ToString());
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient<CustomCommunicationJob>>();
            var resources = client.GetDeploymentOperationListAsync(instance.InstanceId).Result;
            foreach (var r in resources)
            {
                if (r.Name == "resource2")
                {
                    var res = TestHelper.DataConverter.Deserialize<Resource>(r.Input);
                  
                    Assert.Equal(1, res.DependsOn.Count);
                    Assert.Equal("ns.rp/st/resource1",res.DependsOn[0]);
                }
            }
        }

        [Fact(DisplayName = "DuplicatedServiceTypeName")]
        public void DuplicatedServiceTypeName()
        {
            string filename = "dependsOn/DuplicatedServiceTypeName";
            var id = Guid.NewGuid().ToString("N");
            var deployment = new Deployment()
            {
                Template = TestHelper.GetTemplateContent(filename),
                Parameters = string.Empty,
                CorrelationId = Guid.NewGuid().ToString("N"),
                Name = filename.Replace('/', '-'),
                SubscriptionId = Guid.NewGuid().ToString(),
                ResourceGroup = TestHelper.ResourceGroup,
                DeploymentId = id,
                GroupId = Guid.NewGuid().ToString("N"),
                GroupType = "ResourceGroup",
                HierarchyId = "001002003004005",
                CreateByUserId = TestHelper.CreateByUserId,
                ApiVersion = "1.0",
                TenantId = TestHelper.TenantId
            };
            var (rtv, m) = deployment.Validate(fixture.ServiceProvider);
            Assert.True(rtv);
            Assert.Equal(1, deployment.Template.Resources["resource2"].DependsOn.Count);
            Assert.Equal("ns.rp/st/resource1", deployment.Template.Resources["resource2"].DependsOn[0]);
            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/DuplicatedServiceTypeName", subscriptionId: Guid.NewGuid().ToString("N"));
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient<CustomCommunicationJob>>();
            var operations = client.GetDeploymentOperationListAsync(instance.InstanceId).Result;
            foreach (var r in operations)
            {
                Assert.Equal(ProvisioningStage.Successed, r.Stage);
            }
        }

        [Fact(DisplayName = "DiffServiceTypeSameName")]
        public void DiffServiceTypeSameName()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/DiffServiceTypeSameName", subscriptionId: Guid.NewGuid().ToString());
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient<CustomCommunicationJob>>();
            var operations = client.GetDeploymentOperationListAsync(instance.InstanceId).Result;
            foreach (var r in operations)
            {
                Assert.Equal(ProvisioningStage.Successed, r.Stage);
            }
        }

        [Fact(DisplayName = "DependsOnFullname")]
        public void DependsOnFullname()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/dependsOnFullname", subscriptionId: Guid.NewGuid().ToString());
        }
    }
}