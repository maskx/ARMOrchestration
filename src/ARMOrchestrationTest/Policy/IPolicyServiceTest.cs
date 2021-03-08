using maskx.ARMOrchestration.ARMTemplate;
using System;
using Xunit;

namespace ARMOrchestrationTest.Policy
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "PolicyService")]
    public class IPolicyServiceTest
    {
        private readonly ARMOrchestartionFixture fixture;

        public IPolicyServiceTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }
        [Fact(DisplayName = "EvaluateResource")]
        public void EvaluateResource()
        {
            var instance = TestHelper.OrchestrationTest(fixture, "policy/ResourceIteration", Guid.NewGuid().ToString("N"));
            var rs = this.fixture.ARMOrchestrationClient.GetDeploymentOperationListAsync(instance.InstanceId).Result;
            foreach (var r in rs)
            {
               
                if (r.Type == "Microsoft.Storage/storageAccounts")
                {
                    var res = TestHelper.DataConverter.Deserialize<Resource>(r.Input);
                    if (r.Name == "1Policy")
                    {
                        Assert.Equal("{\"comment\":\"policy modified\"}",res.Properties);
                    }
                    else
                    {
                        Assert.Equal("{}",res.Properties);
                    }
                }
            }
        }
    }
}
