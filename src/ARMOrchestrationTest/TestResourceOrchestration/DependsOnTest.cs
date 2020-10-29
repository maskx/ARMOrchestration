using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.InteropServices;
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
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient>();
            var resources = client.GetResourceListAsync(instance.InstanceId).Result;
            foreach (var r in resources)
            {
                if (r.Name == "resource2")
                {
                    var input = TestHelper.DataConverter.Deserialize<ResourceOrchestrationInput>(r.Input);
                    input.ServiceProvider = fixture.ServiceProvider;
                    Assert.Equal(1, input.Resource.DependsOn.Count);
                    Assert.Equal("ns.rp/st/resource1", input.Resource.DependsOn[0]);
                }
            }
        }

        [Fact(DisplayName = "NameAndServiceTypeName")]
        public void NameAndServiceTypeName()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/NameAndServiceTypeName", subscriptionId: Guid.NewGuid().ToString());
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient>();
            var resources = client.GetResourceListAsync(instance.InstanceId).Result;
            foreach (var r in resources)
            {
                if (r.Name == "resource2")
                {
                    var input = TestHelper.DataConverter.Deserialize<ResourceOrchestrationInput>(r.Input);
                    input.ServiceProvider = fixture.ServiceProvider;
                    Assert.Equal(1, input.Resource.DependsOn.Count);
                    Assert.Equal("ns.rp/st/resource1", input.Resource.DependsOn[0]);
                }
            }
        }

        [Fact(DisplayName = "ServiceTypeNameAndName")]
        public void ServiceTypeNameAndName()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/ServiceTypeNameAndName", subscriptionId: Guid.NewGuid().ToString());
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient>();
            var resources = client.GetResourceListAsync(instance.InstanceId).Result;
            foreach (var r in resources)
            {
                if (r.Name == "resource2")
                {
                    var input = TestHelper.DataConverter.Deserialize<ResourceOrchestrationInput>(r.Input);
                    input.ServiceProvider = fixture.ServiceProvider;
                    Assert.Equal(1, input.Resource.DependsOn.Count);
                    Assert.Equal("ns.rp/st/resource1", input.Resource.DependsOn[0]);
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

            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/DuplicatedServiceTypeName", subscriptionId: Guid.NewGuid().ToString("N"));
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient>();
            var resources = client.GetResourceListAsync(instance.InstanceId).Result;
            foreach (var r in resources)
            {
                if (r.Name == "resource2")
                {
                    var input = TestHelper.DataConverter.Deserialize<ResourceOrchestrationInput>(r.Input);
                    input.ServiceProvider = fixture.ServiceProvider;
                    Assert.Equal(1, input.Resource.DependsOn.Count);
                    Assert.Equal("ns.rp/st/resource1", input.Resource.DependsOn[0]);
                }
            }
        }

        [Fact(DisplayName = "DiffServiceTypeSameName")]
        public void DiffServiceTypeSameName()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/DiffServiceTypeSameName", subscriptionId: Guid.NewGuid().ToString());
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient>();
            var resources = client.GetResourceListAsync(instance.InstanceId).Result;
            foreach (var r in resources)
            {
                if (r.Name == "resource2")
                {
                    var input = TestHelper.DataConverter.Deserialize<ResourceOrchestrationInput>(r.Input);
                    input.ServiceProvider = fixture.ServiceProvider;
                    Assert.Equal(2, input.Resource.DependsOn.Count);
                    Assert.Contains("ns.rp/st/resource1", input.Resource.DependsOn);
                    Assert.Contains("ns.rp/st1/resource1", input.Resource.DependsOn);
                }
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