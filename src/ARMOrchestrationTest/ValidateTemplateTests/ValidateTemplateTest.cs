using ARMCreatorTest;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Orchestrations;
using System.Text.Json;
using Xunit;
using System.Linq;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using maskx.OrchestrationService;
using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration.ARMTemplate;
using System;

namespace ARMOrchestrationTest.ValidateTemplateTests
{
    [Trait("c", "ValidateTemplate")]
    public class ValidateTemplateTest
    {
        private ARMTemplateHelper templateHelper = new ARMTemplateHelper(
            Options.Create(new ARMOrchestrationOptions()),
            new ARMFunctions(
                Options.Create(new ARMOrchestrationOptions()),
                null,
                new MockInfrastructure()),
            null,
            new MockInfrastructure());

        private string GetTemplate(string filename)
        {
            return TestHelper.GetJsonFileContent("ValidateTemplateTests/Template/" + filename);
        }

        [Fact(DisplayName = "EmptyTemplate")]
        public void EmptyTemplate()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                TemplateContent = GetTemplate("Empty")
            });
            Assert.True(r.Result);
        }

        [Fact(DisplayName = "NoSchema")]
        public void NoSchema()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                TemplateContent = GetTemplate("NoSchema")
            });
            Assert.False(r.Result);
            Assert.Equal("not find $schema in template", r.Message);
        }

        [Fact(DisplayName = "NoContentVersion")]
        public void NoContentVersion()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                TemplateContent = GetTemplate("NoContentVersion")
            });
            Assert.False(r.Result);
            Assert.Equal("not find contentVersion in template", r.Message);
        }

        [Fact(DisplayName = "NoResources")]
        public void NoResources()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                TemplateContent = GetTemplate("NoResources")
            });
            Assert.False(r.Result);
            Assert.Equal("not find resources in template", r.Message);
        }

        [Fact(DisplayName = "VariableIteration")]
        public void VariableIteration()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                TemplateContent = TestHelper.GetJsonFileContent("Templates/CopyIndex/VariableIteration")
            });
            Assert.True(r.Result);
            using var doc = JsonDocument.Parse(r.Deployment.Template.Variables);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("disk-array-on-object", out JsonElement ele1));
            Assert.True(ele1.TryGetProperty("disks", out JsonElement disks));
            Assert.True(ele1.TryGetProperty("diskNames", out JsonElement diskNames));
            Assert.Equal(5, disks.GetArrayLength());
            Assert.Equal(5, diskNames.GetArrayLength());
            Assert.True(root.TryGetProperty("top-level-object-array", out JsonElement ele2));
            Assert.Equal(5, ele2.GetArrayLength());
            Assert.True(root.TryGetProperty("top-level-string-array", out JsonElement ele3));
            Assert.Equal(5, ele3.GetArrayLength());
            Assert.True(root.TryGetProperty("top-level-integer-array", out JsonElement ele4));
            Assert.Equal(5, ele4.GetArrayLength());
        }

        [Fact(DisplayName = "ResourceIteration")]
        public void ResourceIteration()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                TemplateContent = TestHelper.GetJsonFileContent("Templates/CopyIndex/ResourceIteration")
            });
            Assert.True(r.Result);
            Assert.True(r.Deployment.Template.Resources.ContainsKey("storagecopy"));
            Assert.Equal(3, r.Deployment.Template.Resources["storagecopy"].Resources.Count);
            var resource = r.Deployment.Template.Resources["storagecopy"].Resources.First();
            Assert.Equal("0storage", resource);
            Assert.Equal(4, r.Deployment.Template.Resources.Count);
            Assert.Equal("Microsoft.Storage/storageAccounts", r.Deployment.Template.Resources["0storage"].Type);
        }

        [Fact(DisplayName = "PropertyIteration")]
        public void PropertyIteration()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                TemplateContent = TestHelper.GetJsonFileContent("Templates/CopyIndex/PropertyIteration")
            });
            Assert.True(r.Result);
            Assert.Single(r.Deployment.Template.Resources);
            var resource = r.Deployment.Template.Resources.Values.First();
            using var doc = JsonDocument.Parse(resource.Properties);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("storageProfile", out JsonElement storageProfile));
            Assert.True(storageProfile.TryGetProperty("dataDisks", out JsonElement dataDisks));
            Assert.Equal(3, dataDisks.GetArrayLength());
            int index = 0;
            foreach (var item in dataDisks.EnumerateArray())
            {
                Assert.True(item.TryGetProperty("lun", out JsonElement lun));
                Assert.Equal(index, lun.GetInt32());
                index++;
            }
        }

        [Fact(DisplayName = "ChildResource")]
        public void ChildResource()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                TemplateContent = GetTemplate("ChildResource")
            });
            Assert.True(r.Result);
            Assert.Equal(2, r.Deployment.Template.Resources.Count);
            Assert.True(r.Deployment.Template.Resources.TryGetValue("VNet1", out Resource v));
            Assert.True(r.Deployment.Template.Resources.TryGetValue("Subnet1", out Resource s));
            Assert.Equal("Microsoft.Network/virtualNetworks", v.FullType);
            Assert.Equal("Microsoft.Network/virtualNetworks/subnets", s.FullType);
        }

        [Fact(DisplayName = "NestTemplate")]
        public void NestTemplate()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                TemplateContent = GetTemplate("NestTemplate"),
                DeploymentId = Guid.NewGuid().ToString("N"),
                GroupId = Guid.NewGuid().ToString("N"),
                GroupType = "ResourceGroup",
                HierarchyId = "001002003004005"
            });
            Assert.True(r.Result);
            Assert.Single(r.Deployment.Deployments);
            var d = r.Deployment.Deployments[0];
            Assert.Equal("nestedTemplate1", d.DeploymentName);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.Equal(r.Deployment.RootId, d.RootId);
            Assert.NotNull(d.DeploymentId);

            Assert.NotNull(d.Template);
            var t = d.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.Values.First();
            Assert.Equal("storageAccount1", res.FullName);
            Assert.Equal("Microsoft.Storage/storageAccounts", res.FullType);
        }

        [Fact(DisplayName = "ExpressionEvaluationScopeInner")]
        public void ExpressionEvaluationScopeInner()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                TemplateContent = GetTemplate("ExpressionsInNestedTemplates-inner")
            });
            Assert.True(r.Result);
            Assert.Single(r.Deployment.Deployments);
            var d = r.Deployment.Deployments[0];
            Assert.Equal("nestedTemplate1", d.DeploymentName);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.NotNull(d.Template);
            var t = d.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.Values.First();
            Assert.Equal("from nested template", res.FullName);
        }

        [Fact(DisplayName = "ExpressionEvaluationScopeOuter")]
        public void ExpressionEvaluationScopeOuter()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                TemplateContent = GetTemplate("ExpressionsInNestedTemplates-outer")
            });
            Assert.True(r.Result);
            Assert.Single(r.Deployment.Deployments);
            var d = r.Deployment.Deployments[0];
            Assert.Equal("nestedTemplate1", d.DeploymentName);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.NotNull(d.Template);
            var t = d.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.Values.First();
            Assert.Equal("from parent template", res.FullName);
        }
    }
}