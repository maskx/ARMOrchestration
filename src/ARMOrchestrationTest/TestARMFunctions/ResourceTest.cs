using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
            string subscriptionId = Guid.NewGuid().ToString();
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"WithSubscriptionId",$"/subscription/11645A35-036C-48F0-BD7F-EA8312B8DC18/providers/Microsoft.Authorization/locks/lockname1"},
                {"WithOutSubscriptionId",$"/subscription/{subscriptionId}/providers/Microsoft.Authorization/locks/lockname1"},
                {"NestResource",$"/subscription/{subscriptionId}/providers/Microsoft.Authorization/locks/lockname1/nestResourceType/NestResrouceName"}
             };
            TestHelper.FunctionTest(this.fixture, "subscriptionResourceId", result, subscriptionId);
        }

        [Fact(DisplayName = "ManagementGroupResourceid")]
        public void ManagementGroupResourceid()
        {
            var mgid = Guid.NewGuid().ToString();
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"WithManagementGroupId",$"/management/11645A35-036C-48F0-BD7F-EA8312B8DC18/providers/Microsoft.Authorization/locks/lockname1"},
                {"WithOutManagementGroupId",$"/management/{mgid}/providers/Microsoft.Authorization/locks/lockname1"},
                {"NestResource",$"/management/{mgid}/providers/Microsoft.Authorization/locks/lockname1/nestResourceType/NestResrouceName"}
             };
            TestHelper.FunctionTest(this.fixture, "ManagementResourceid", result, managementGroupId: mgid);
        }

        [Fact(DisplayName = "resourceid")]
        public void Resourceid()
        {
            string subscriptionId = Guid.NewGuid().ToString();
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"sameRGOutput",$"/subscription/{subscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                {"differentRGOutput",$"/subscription/{subscriptionId}/resourceGroups/otherResourceGroup/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                {"differentSubOutput","/subscription/11111111-1111-1111-1111-111111111111/resourceGroups/otherResourceGroup/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                { "nestedResourceOutput",$"/subscription/{subscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.SQL/servers/serverName/databases/databaseName"}
            };
            TestHelper.FunctionTest(this.fixture, "resourceid",result,subscriptionId);
        }

        [Fact(DisplayName = "resourceidInternl")]
        public void ResourceidInternl()
        {
            var func = this.fixture.ServiceProvider.GetService<ARMFunctions>();
            Deployment context = new Deployment();
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
                    {"armcontext",new Deployment(){
                        Template="{}" } }
                }); ;
            Assert.NotNull(rtv);
        }

        [Fact(DisplayName = "ListResourceInPrepareTime")]
        public void ListResourceInPrepareTime()
        {
            Dictionary<string, object> cxt = new Dictionary<string, object>() {
                    {"armcontext",new Deployment(){
                        Template="{ }" ,
                        IsRuntime=false}}
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
    }
}