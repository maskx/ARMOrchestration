using ARMCreatorTest;
using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ARMOrchestrationTest.TestARMFunctions
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("ARMFunctions", "Resource")]
    public class ResourceTest
    {
        private ARMOrchestartionFixture fixture;

        public ResourceTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "extensionResourceId")]
        public void extensionResourceId()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"lockResourceId",$"/subscription/{TestHelper.SubscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.Authorization/locks/lockname1/"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "extensionResourceId", result);
        }

        [Fact(DisplayName = "resourceid")]
        public void resourceid()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"sameRGOutput",$"/subscription/{TestHelper.SubscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                {"differentRGOutput",$"/subscription/{TestHelper.SubscriptionId}/resourceGroups/otherResourceGroup/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                {"differentSubOutput","/subscription/11111111-1111-1111-1111-111111111111/resourceGroups/otherResourceGroup/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                { "nestedResourceOutput",$"/subscription/{TestHelper.SubscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.SQL/servers/serverName/databases/databaseName"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "resourceid", result);
        }

        [Fact(DisplayName = "list*")]
        public void ListResource()
        {
            ARMFunctions functions = new ARMFunctions(
                Options.Create(new ARMOrchestrationOptions()),
                null,
                new MockInfrastructure(null));
            object rtv = functions.Evaluate(
                "[listId('resourceId','2019-01-02')]",
                new Dictionary<string, object>() {
                    {"armcontext",new DeploymentContext(){
                        Template=new Template() } }
                });
            Assert.NotNull(rtv);
        }

        [Fact(DisplayName = "resourceGroup")]
        public void resourceGroup()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"resourceGroupOutput",
                    JObject
                .Parse(TestHelper.GetJsonFileContent("mock/response/resourcegroup1"))
                .ToString(Newtonsoft.Json.Formatting.None)}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "resourceGroup", result);
        }

        [Fact(DisplayName = "ReferenceNoDependsOn")]
        public void ReferenceNoDependsOn()
        {
            var full = JObject.Parse(TestHelper.GetJsonFileContent("mock/response/examplestorage"));

            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"referenceOutput",full["properties"].ToString(Newtonsoft.Json.Formatting.None)},
                {"fullReferenceOutput",full.ToString(Newtonsoft.Json.Formatting.None) }
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "reference/reference", result);
        }

        [Fact(DisplayName = "ReferenceDependsOn")]
        public void ReferenceDependsOn()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
            };
            var instance = TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "reference/referenceDependsOn", result);
            var rs = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            bool hasResource = false;
            foreach (var r in rs)
            {
                if (r.Name == "ReferenceInProperty")
                {
                    hasResource = true;
                    var input = TestHelper.DataConverter.Deserialize<WaitDependsOnActivityInput>(r.Input);
                    var p = JsonDocument.Parse(input.Resource.Properties);
                    var c = p.RootElement.GetProperty("comment");
                    Assert.Equal("Succeeded2020-3-11", c.GetString());
                }
            }
            Assert.True(hasResource);
        }

        [Fact(DisplayName = "ReferenceResourceIteration")]
        public void ReferenceResourceIteration()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
            };
            var instance = TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "reference/ResourceIteration", result);
            var rs = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            Assert.Equal(6, rs.Count);
            int copyCount = 0;
            bool hasDependsOnResource = false;
            bool hasDeployments = false;
            bool hasCopy = false;
            foreach (var r in rs)
            {
                if (r.Type == "Microsoft.Resources/deployments")
                    hasDeployments = true;
                if (r.Type == "Copy")
                    hasCopy = true;
                if (r.Type == "Microsoft.Storage/storageAccounts1")
                    hasDependsOnResource = true;
                if (r.Type == "Microsoft.Storage/storageAccounts")
                {
                    copyCount++;
                    Assert.True(int.TryParse(r.Name, out int i));
                    var input = TestHelper.DataConverter.Deserialize<WaitDependsOnActivityInput>(r.Input);
                    var p = JsonDocument.Parse(input.Resource.Properties);
                    var c = p.RootElement.GetProperty("comment");
                    Assert.Equal("Succeeded" + i, c.GetString());
                }
            }
            Assert.True(hasCopy);
            Assert.True(hasDeployments);
            Assert.True(hasDependsOnResource);
            Assert.Equal(3, copyCount);
        }

        [Fact(DisplayName = "ReferencePropertyIteration")]
        public void ReferencePropertyIteration()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
            };
            var instance = TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "reference/PropertyIteration", result);
            var rs = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            bool hasexamplevm = false;
            int diskCount = 0;
            foreach (var r in rs)
            {
                if (r.Name == "examplevm")
                {
                    hasexamplevm = true;
                    var input = TestHelper.DataConverter.Deserialize<WaitDependsOnActivityInput>(r.Input);
                    var p = JsonDocument.Parse(input.Resource.Properties);
                    Assert.True(p.RootElement.TryGetProperty("storageProfile", out JsonElement storageProfile));
                    Assert.True(storageProfile.TryGetProperty("dataDisks", out JsonElement dataDisks));
                    foreach (var d in dataDisks.EnumerateArray())
                    {
                        diskCount++;
                        var c = d.GetProperty("comment");
                        Assert.Equal("Succeeded" + d.GetProperty("lun").GetInt32(), c.GetString());
                    }
                }
            }
            Assert.True(hasexamplevm);
            Assert.Equal(3, diskCount);
        }
    }
}