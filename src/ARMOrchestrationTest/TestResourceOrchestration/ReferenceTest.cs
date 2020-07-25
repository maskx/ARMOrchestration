﻿using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("ARMFunctions", "Reference")]
    [Trait("c", "ARMFunctions")]
    public class ReferenceTest
    {
        private readonly ARMOrchestartionFixture fixture;

        public ReferenceTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
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

        [Fact(DisplayName = "IncluedServiceTypeDependsOn")]
        public void IncluedServiceTypeDependsOn()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
            };
            var instance = TestHelper.FunctionTest(this.fixture, "reference/IncluedServiceTypeDependsOn", result);
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
        [Fact(DisplayName = "IncluedServiceTypeNotExist")]
        public void IncluedServiceTypeNotExist()
        {
            var (instance, taskResult) = TestHelper.FunctionTestNotCheckResult(this.fixture, "reference/IncluedServiceTypeNotExist");
            Assert.NotEqual(200, taskResult.Code);
            Assert.Equal("cannot find dependson resource named 'Microsoft.Storage/storageAccounts1/examplestorage'", taskResult.Content);
            var resourceList = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result;
            foreach (var r in resourceList)
            {
               var tr= TestHelper.DataConverter.Deserialize<TaskResult>(r.Result);
                Assert.NotEqual(200, tr.Code);
                Assert.Equal("cannot find dependson resource named 'Microsoft.Storage/storageAccounts1/examplestorage'", tr.Content);

            }
        }
        [Fact(DisplayName = "2ReferenceDependsOn")]
        public void TwoReferenceDependsOn()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
            };
            var instance = TestHelper.FunctionTest(this.fixture, "reference/2referenceDependsOn", result);
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
                    var c2 = p.RootElement.GetProperty("comment2");
                    Assert.Equal("Completed222", c2.GetString());
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
                    using var p = JsonDocument.Parse(input.Resource.Properties);
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