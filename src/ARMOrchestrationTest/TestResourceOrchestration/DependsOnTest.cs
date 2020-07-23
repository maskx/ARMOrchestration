using ARMCreatorTest;
using DurableTask.Core;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Orchestrations;

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
                "dependsOn/OneResourceName");
        }

        [Fact(DisplayName = "ThreeResourceName")]
        public void ThreeResourceName()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/ThreeResourceName");
        }

        [Fact(DisplayName = "CopyLoop")]
        public void CopyLoop()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/copyloop");
        }

        [Fact(DisplayName = "ConditionFalse")]
        public void ConditionFalse()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/ConditionFalse");
        }
        [Fact(DisplayName = "DeployDepondsOn")]
        public void DeployDepondsOn()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/DeployDepondsOn");
        }
        [Fact(DisplayName = "DuplicatedName")]
        public void DuplicatedName()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/DuplicatedName");
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient>();
            var resources = client.GetResourceListAsync(instance.InstanceId).Result;
            foreach (var r in resources)
            {
                if (r.Name == "resource2")
                {
                    var input = TestHelper.DataConverter.Deserialize<ResourceOrchestrationInput>(r.Input);
                    Assert.Single(input.Resource.DependsOn);
                    Assert.Equal("resource1", input.Resource.DependsOn[0]);
                }
            }
        }
        [Fact(DisplayName = "NameAndServiceTypeName")]
        public void NameAndServiceTypeName()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                 "dependsOn/NameAndServiceTypeName");
            var client = fixture.ServiceProvider.GetService<ARMOrchestrationClient>();
            var resources = client.GetResourceListAsync(instance.InstanceId).Result;
            foreach (var r in resources)
            {
                if (r.Name == "resource2")
                {
                    var input = TestHelper.DataConverter.Deserialize<ResourceOrchestrationInput>(r.Input);
                    Assert.Single(input.Resource.DependsOn);
                    Assert.Equal("resource1", input.Resource.DependsOn[0]);
                }
            }
        }
        [Fact(DisplayName = "DependsOnFullname")]
        public void DependsOnFullname()
        {
            TestHelper.OrchestrationTest(fixture,
                "dependsOn/dependsOnFullname");
        }
    }
}