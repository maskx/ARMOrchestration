﻿using ARMCreatorTest;
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
                Template = GetTemplate("Empty")
            });
            Assert.True(r.Result);
        }

        [Fact(DisplayName = "NoSchema")]
        public void NoSchema()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                Template = GetTemplate("NoSchema")
            });
            Assert.False(r.Result);
            Assert.Equal("not find $schema in template", r.Message);
        }

        [Fact(DisplayName = "NoContentVersion")]
        public void NoContentVersion()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                Template = GetTemplate("NoContentVersion")
            });
            Assert.False(r.Result);
            Assert.Equal("not find contentVersion in template", r.Message);
        }

        [Fact(DisplayName = "NoResources")]
        public void NoResources()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                Template = GetTemplate("NoResources")
            });
            Assert.False(r.Result);
            Assert.Equal("not find resources in template", r.Message);
        }

        [Fact(DisplayName = "VariableIteration")]
        public void VariableIteration()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/VariableIteration")
            });
            Assert.True(r.Result);
            using var doc = JsonDocument.Parse(r.Deployment.TemplateOjbect.Variables);
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
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/ResourceIteration")
            });
            Assert.True(r.Result);
            Assert.True(r.Deployment.TemplateOjbect.Resources.ContainsKey("storagecopy"));
            Assert.Equal(3, r.Deployment.TemplateOjbect.Resources["storagecopy"].Resources.Count);
            var resource = r.Deployment.TemplateOjbect.Resources["storagecopy"].Resources.First();
            Assert.Equal("0storage", resource);
            Assert.Equal(4, r.Deployment.TemplateOjbect.Resources.Count);
            Assert.Equal("Microsoft.Storage/storageAccounts", r.Deployment.TemplateOjbect.Resources["0storage"].Type);
        }

        [Fact(DisplayName = "PropertyIteration")]
        public void PropertyIteration()
        {
            var r = templateHelper.ParseDeployment(new DeploymentOrchestrationInput()
            {
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/PropertyIteration")
            });
            Assert.True(r.Result);
            Assert.Single(r.Deployment.TemplateOjbect.Resources);
            var resource = r.Deployment.TemplateOjbect.Resources.Values.First();
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
                Template = GetTemplate("ChildResource")
            });
            Assert.True(r.Result);
            Assert.Equal(2, r.Deployment.TemplateOjbect.Resources.Count);
            Assert.True(r.Deployment.TemplateOjbect.Resources.TryGetValue("VNet1", out Resource v));
            Assert.True(r.Deployment.TemplateOjbect.Resources.TryGetValue("Subnet1", out Resource s));
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
                Template = GetTemplate("NestTemplate")
            });
            Assert.True(r.Result);
            Assert.Single(r.Deployment.Deployments);
            var d = r.Deployment.Deployments[0];
            Assert.Equal("nestedTemplate1", d.Name);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.NotNull(d.TemplateOjbect);
            var t = d.TemplateOjbect;
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
                Template = GetTemplate("ExpressionsInNestedTemplates-inner")
            });
            Assert.True(r.Result);
            Assert.Single(r.Deployment.Deployments);
            var d = r.Deployment.Deployments[0];
            Assert.Equal("nestedTemplate1", d.Name);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.NotNull(d.TemplateOjbect);
            var t = d.TemplateOjbect;
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
                Template = GetTemplate("ExpressionsInNestedTemplates-outer")
            });
            Assert.True(r.Result);
            Assert.Single(r.Deployment.Deployments);
            var d = r.Deployment.Deployments[0];
            Assert.Equal("nestedTemplate1", d.Name);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.NotNull(d.TemplateOjbect);
            var t = d.TemplateOjbect;
            Assert.Single(t.Resources);
            var res = t.Resources.Values.First();
            Assert.Equal("from parent template", res.FullName);
        }
    }
}