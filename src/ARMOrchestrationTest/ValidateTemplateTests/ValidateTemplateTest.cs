using ARMOrchestrationTest;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace ARMOrchestrationTest.ValidateTemplateTests
{
    [Trait("c", "ValidateTemplate")]
    [Collection("WebHost ARMOrchestartion")]
    public class ValidateTemplateTest
    {
        private readonly ARMFunctions functions;
        private readonly IInfrastructure infrastructure;
        private readonly ARMOrchestartionFixture fixture;

        public ValidateTemplateTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
            this.functions = fixture.ARMFunctions;
            this.infrastructure = fixture.ServiceProvider.GetService<IInfrastructure>();
        }

        private string GetTemplate(string filename)
        {
            return TestHelper.GetJsonFileContent("ValidateTemplateTests/Template/" + filename);
        }

        [Fact(DisplayName = "EmptyTemplate")]
        public void EmptyTemplate()
        {
            var r = DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
            {
                DeploymentName = "EmptyTemplate",
                Template = GetTemplate("Empty"),
                ServiceProvider = fixture.ServiceProvider
            }, functions, infrastructure);
            Assert.NotNull(r);
            Assert.Empty(r.Template.Resources);
            Assert.Empty(r.Deployments);
            var s = r.Template.ToString();
            using var doc = JsonDocument.Parse(s);
            var root = doc.RootElement;
        }

        [Fact(DisplayName = "NoSchema")]
        public void NoSchema()
        {
            Assert.Equal("not find $schema in template",
                Assert.Throws<Exception>(() =>
                {
                    DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
                    {
                        DeploymentName = "NoSchema",
                        Template = GetTemplate("NoSchema"),
                        ServiceProvider = fixture.ServiceProvider
                    }, functions, infrastructure);
                }).Message);
        }

        [Fact(DisplayName = "NoContentVersion")]
        public void NoContentVersion()
        {
            Assert.Equal("not find contentVersion in template",
               Assert.Throws<Exception>(() =>
               {
                   DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
                   {
                       DeploymentName = "NoContentVersion",
                       Template = GetTemplate("NoContentVersion"),
                       ServiceProvider = fixture.ServiceProvider
                   }, functions, infrastructure);
               }).Message);
        }

        [Fact(DisplayName = "NoResources")]
        public void NoResources()
        {
            Assert.Equal("not find resources in template",
               Assert.Throws<Exception>(() =>
               {
                   DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
                   {
                       DeploymentName = "NoResources",
                       Template = GetTemplate("NoResources"),
                       ServiceProvider = fixture.ServiceProvider
                   }, functions, infrastructure);
               }).Message);
        }

        [Fact(DisplayName = "VariableIteration")]
        public void VariableIteration()
        {
            var Deployment = DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
            {
                DeploymentName = "VariableIteration",
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/VariableIteration"),
                ServiceProvider = fixture.ServiceProvider
            }, functions, infrastructure);

            using var doc = JsonDocument.Parse(Deployment.Template.Variables);
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

            var s = Deployment.Template.ToString();
            using var doc1 = JsonDocument.Parse(s);
            var root1 = doc1.RootElement;
        }

        [Fact(DisplayName = "ResourceIteration")]
        public void ResourceIteration()
        {
            var Deployment = DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
            {
                DeploymentName = "ResourceIteration",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/ResourceIteration"),
                ServiceProvider = fixture.ServiceProvider
            }, functions, infrastructure);

            Assert.True(Deployment.Template.Resources.ContainsKey("storagecopy"));
            // TODO: need modify test code
            //Assert.Equal(3, Deployment.Template.Resources["storagecopy"].Resources.Count);
            //var resource = Deployment.Template.Resources["storagecopy"].Resources.First();
            //Assert.Equal("0storage", resource);
            //Assert.Equal(4, Deployment.Template.Resources.Count);
            //Assert.Equal("Microsoft.Storage/storageAccounts", Deployment.Template.Resources["0storage"].Type);

            var s = Deployment.Template.ToString();
            using var doc1 = JsonDocument.Parse(s);
            var root1 = doc1.RootElement;
        }

        [Fact(DisplayName = "PropertyIteration")]
        public void PropertyIteration()
        {
            var Deployment = DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
            {
                DeploymentName = "PropertyIteration",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/PropertyIteration"),
                ServiceProvider = fixture.ServiceProvider
            }, functions, infrastructure);

            Assert.Single(Deployment.Template.Resources);
            var resource = Deployment.Template.Resources.First();
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
            var s = Deployment.Template.ToString();
            using var doc1 = JsonDocument.Parse(s);
            var root1 = doc1.RootElement;
        }

        [Fact(DisplayName = "ChildResource")]
        public void ChildResource()
        {
            var Deployment = DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
            {
                DeploymentName = "ChildResource",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("ChildResource"),
                ServiceProvider = fixture.ServiceProvider
            }, functions, infrastructure);

            Assert.Equal(2, Deployment.Template.Resources.Count);
            Assert.True(Deployment.Template.Resources.TryGetValue("VNet1", out Resource v));
            Assert.True(Deployment.Template.Resources.TryGetValue("Subnet1", out Resource s));
            Assert.Equal("Microsoft.Network/virtualNetworks", v.FullType);
            Assert.Equal("Microsoft.Network/virtualNetworks/subnets", s.FullType);
            var s1 = Deployment.Template.ToString();
            using var doc1 = JsonDocument.Parse(s1);
            var root1 = doc1.RootElement;
        }

        [Fact(DisplayName = "NestTemplate")]
        public void NestTemplate()
        {
            var Deployment = DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
            {
                DeploymentName = "NestTemplate",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("NestTemplate"),
                DeploymentId = Guid.NewGuid().ToString("N"),
                GroupId = Guid.NewGuid().ToString("N"),
                GroupType = "ResourceGroup",
                HierarchyId = "001002003004005",
                ServiceProvider = fixture.ServiceProvider
            }, functions, infrastructure);

            Assert.Single(Deployment.Deployments);
            var d = Deployment.Deployments.First().Value;
            Assert.Equal("nestedTemplate1", d.DeploymentName);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.Equal(Deployment.RootId, d.RootId);
            Assert.NotNull(d.DeploymentId);

            Assert.NotNull(d.Template);
            var t = d.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.First();
            Assert.Equal("storageAccount1", res.FullName);
            Assert.Equal("Microsoft.Storage/storageAccounts", res.FullType);
            var s = Deployment.Template.ToString();
            using var doc1 = JsonDocument.Parse(s);
            var root1 = doc1.RootElement;
        }

        [Fact(DisplayName = "DoubleNestTemplate")]
        public void DoubleNestTemplate()
        {
            var Deployment = DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
            {
                DeploymentName = "DoubleNestTemplate",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("DoubleNestTemplate"),
                DeploymentId = Guid.NewGuid().ToString("N"),
                GroupId = Guid.NewGuid().ToString("N"),
                GroupType = "ResourceGroup",
                HierarchyId = "001002003004005",
                ServiceProvider = fixture.ServiceProvider
            }, functions, infrastructure);

            Assert.Equal(2, Deployment.Deployments.Count);

            var d1 = Deployment.Deployments["nestedTemplate1"];
            Assert.Equal("2017-05-10", d1.ApiVersion);
            Assert.Equal(Deployment.RootId, d1.RootId);
            Assert.NotNull(d1.DeploymentId);
            Assert.Empty(d1.Deployments);

            Assert.NotNull(d1.Template);
            var t = d1.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.First();
            Assert.Equal("nestedTemplate2", res.FullName);
            Assert.Equal("Microsoft.Resources/deployments", res.FullType);

            var d2 = Deployment.Deployments["nestedTemplate2"];
            Assert.Equal("2017-05-10", d2.ApiVersion);
            Assert.Equal(Deployment.RootId, d2.RootId);
            Assert.NotNull(d2.DeploymentId);
            Assert.Empty(d2.Deployments);

            Assert.NotNull(d2.Template);
            var t2 = d2.Template;
            Assert.Single(t2.Resources);
            var res2 = t2.Resources.First();
            Assert.Equal("storageAccount1", res2.FullName);
            Assert.Equal("Microsoft.Storage/storageAccounts", res2.FullType);
        }

        [Fact(DisplayName = "ExpressionEvaluationScopeInner")]
        public void ExpressionEvaluationScopeInner()
        {
            var Deployment = DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
            {
                DeploymentName = "ExpressionEvaluationScopeInner",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("ExpressionsInNestedTemplates-inner"),
                ServiceProvider = fixture.ServiceProvider
            }, functions, infrastructure);

            Assert.Single(Deployment.Deployments);
            var d = Deployment.Deployments.First().Value;
            Assert.Equal("nestedTemplate1", d.DeploymentName);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.NotNull(d.Template);
            var t = d.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.First();
            Assert.Equal("from nested template", res.FullName);
            var s = Deployment.Template.ToString();
            using var doc1 = JsonDocument.Parse(s);
            var root1 = doc1.RootElement;
        }

        [Fact(DisplayName = "ExpressionEvaluationScopeOuter")]
        public void ExpressionEvaluationScopeOuter()
        {
            var Deployment = DeploymentOrchestrationInput.Validate(new DeploymentOrchestrationInput()
            {
                DeploymentName = "ExpressionEvaluationScopeOuter",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("ExpressionsInNestedTemplates-outer"),
                ServiceProvider = fixture.ServiceProvider
            }, functions, infrastructure);

            Assert.Single(Deployment.Deployments);
            var d = Deployment.Deployments.First().Value;
            Assert.Equal("nestedTemplate1", d.DeploymentName);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.NotNull(d.Template);
            var t = d.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.First();
            Assert.Equal("from parent template", res.FullName);
            var s = Deployment.Template.ToString();
            using var doc1 = JsonDocument.Parse(s);
            var root1 = doc1.RootElement;
        }
    }
}