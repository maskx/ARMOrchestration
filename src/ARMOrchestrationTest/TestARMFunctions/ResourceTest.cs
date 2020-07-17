using ARMCreatorTest;
using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace ARMOrchestrationTest.TestARMFunctions
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("ARMFunctions", "Resource")]
    [Trait("c", "ARMFunctions")]
    public class ResourceTest
    {
        private readonly ARMOrchestartionFixture fixture;

        public ResourceTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "extensionResourceId")]
        public void ExtensionResourceId()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"lockResourceId",$"/subscription/{TestHelper.SubscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.Authorization/locks/lockname1/"}
            };
            TestHelper.FunctionTest(this.fixture, "extensionResourceId", result);
        }
        [Fact(DisplayName = "SubscriptionResourceId")]
        public void SubscriptionResourceId()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"WithSubscriptionId",$"/subscription/11645A35-036C-48F0-BD7F-EA8312B8DC18/providers/Microsoft.Authorization/locks/lockname1"},
                {"WithOutSubscriptionId",$"/subscription/{TestHelper.SubscriptionId}/providers/Microsoft.Authorization/locks/lockname1"},
                {"NestResource",$"/subscription/{TestHelper.SubscriptionId}/providers/Microsoft.Authorization/locks/lockname1/nestResourceType/NestResrouceName"}
             };
            TestHelper.FunctionTest(this.fixture, "subscriptionResourceId", result);
        }
        [Fact(DisplayName = "ManagementGroupResourceid")]
        public void ManagementGroupResourceid()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"WithManagementGroupId",$"/management/11645A35-036C-48F0-BD7F-EA8312B8DC18/providers/Microsoft.Authorization/locks/lockname1"},
                {"WithOutManagementGroupId",$"/management/{TestHelper.ManagemntGroupId}/providers/Microsoft.Authorization/locks/lockname1"},
                {"NestResource",$"/management/{TestHelper.ManagemntGroupId}/providers/Microsoft.Authorization/locks/lockname1/nestResourceType/NestResrouceName"}
             };
            TestHelper.FunctionTest(this.fixture, "ManagementResourceid", result,TestHelper.ManagemntGroupId);
        }
        [Fact(DisplayName = "resourceid")]
        public void Resourceid()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"sameRGOutput",$"/subscription/{TestHelper.SubscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                {"differentRGOutput",$"/subscription/{TestHelper.SubscriptionId}/resourceGroups/otherResourceGroup/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                {"differentSubOutput","/subscription/11111111-1111-1111-1111-111111111111/resourceGroups/otherResourceGroup/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                { "nestedResourceOutput",$"/subscription/{TestHelper.SubscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.SQL/servers/serverName/databases/databaseName"}
            };
            TestHelper.FunctionTest(this.fixture, "resourceid", result);
        }

        [Fact(DisplayName = "resourceidInternl")]
        public void ResourceidInternl()
        {
            var func = this.fixture.ServiceProvider.GetService<ARMFunctions>();
            DeploymentContext context = new DeploymentContext();
            List<object> pars = new List<object>
            {
                TestHelper.SubscriptionId,
                TestHelper.ResourceGroup,
                "rp/t1/t2/t3/t4"
            };
            pars.AddRange("r1/r2/r3/r4".Split('/'));
            var id = func.ResourceId(context, pars.ToArray());
            Assert.Equal("/subscription/c1fa36c2-4d58-45e8-9c51-498fadb4d8bf/resourceGroups/ResourceGroup1/providers/rp/t1/r1/t2/r2/t3/r3/t4/r4", id);
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

        [Fact(DisplayName = "ListResourceInPrepareTime")]
        public void ListResourceInPrepareTime()
        {
            Dictionary<string, object> cxt = new Dictionary<string, object>() {
                    {"armcontext",new DeploymentContext(){
                        Template=new Template() } },{ContextKeys.IS_PREPARE,true }
                };
            ARMFunctions functions = new ARMFunctions(
                Options.Create(new ARMOrchestrationOptions()),
                null,
                new MockInfrastructure(null));
            object rtv = functions.Evaluate("[listId('resourceId','2019-01-02')]", cxt);
            Assert.NotNull(rtv);
            Assert.True(cxt.TryGetValue(ContextKeys.DEPENDSON, out object depend));
            var dList = depend as List<string>;
            Assert.Contains("resourceId", dList);
        }

        [Fact(DisplayName = "resourceGroup")]
        public void ResourceGroup()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"resourceGroupOutput",
                    JObject
                .Parse(TestHelper.GetJsonFileContent("mock/response/resourcegroup1"))
                .ToString(Newtonsoft.Json.Formatting.None)}
            };
            TestHelper.FunctionTest(this.fixture, "resourceGroup", result);
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
            TestHelper.FunctionTest(this.fixture, "reference/reference", result);
        }

        [Fact(DisplayName = "ReferenceDependsOn")]
        public void ReferenceDependsOn()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
            };
            var instance = TestHelper.FunctionTest(this.fixture, "reference/referenceDependsOn", result);
            var rs = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            bool hasResource = false;
            foreach (var r in rs)
            {
                if (r.Name == "ReferenceInProperty")
                {
                    hasResource = true;
                    var input = TestHelper.DataConverter.Deserialize<ResourceOrchestrationInput>(r.Input);
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
            var instance = TestHelper.FunctionTest(this.fixture, "reference/ResourceIteration", result);
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
                if (r.Type == "Microsoft.Resources/deployments/copy")
                    hasCopy = true;
                if (r.Type == "Microsoft.Storage/storageAccounts1")
                    hasDependsOnResource = true;
                if (r.Type == "Microsoft.Storage/storageAccounts")
                {
                    copyCount++;
                    Assert.True(int.TryParse(r.Name, out int i));
                    var input = TestHelper.DataConverter.Deserialize<ResourceOrchestrationInput>(r.Input);
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
            var instance = TestHelper.FunctionTest(this.fixture, "reference/PropertyIteration", result);
            var rs = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            bool hasexamplevm = false;
            int diskCount = 0;
            foreach (var r in rs)
            {
                if (r.Name == "examplevm")
                {
                    hasexamplevm = true;
                    var input = TestHelper.DataConverter.Deserialize<ResourceOrchestrationInput>(r.Input);
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