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
            var r = new DeploymentOrchestrationInput()
            {
                DeploymentName = "EmptyTemplate",
                Template = GetTemplate("Empty"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (v, m) = r.Validate(fixture.ServiceProvider);
            Assert.True(v);
            Assert.Empty(m);
            Assert.NotNull(r);
            Assert.Empty(r.Template.Resources);
        }

        [Fact(DisplayName = "NoSchema")]
        public void NoSchema()
        {
            var input = new DeploymentOrchestrationInput()
            {
                DeploymentName = "NoSchema",
                Template = GetTemplate("NoSchema"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = input.Validate(fixture.ServiceProvider);
            Assert.False(r);
            Assert.Equal("not find $schema in template", m);
        }

        [Fact(DisplayName = "NoContentVersion")]
        public void NoContentVersion()
        {
            var input = new DeploymentOrchestrationInput()
            {
                DeploymentName = "NoContentVersion",
                Template = GetTemplate("NoContentVersion"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = input.Validate();
            Assert.False(r);
            Assert.Equal("not find contentVersion in template", m);
        }

        [Fact(DisplayName = "NoResources")]
        public void NoResources()
        {
            var input = new DeploymentOrchestrationInput()
            {
                DeploymentName = "NoResources",
                Template = GetTemplate("NoResources"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = input.Validate(fixture.ServiceProvider);
            Assert.False(r);
            Assert.Equal("not find resources in template", m);
        }

        [Fact(DisplayName = "VariableIteration")]
        public void VariableIteration()
        {
            var input = new DeploymentOrchestrationInput()
            {
                DeploymentName = "VariableIteration",
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/VariableIteration"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = input.Validate();
            Assert.True(r);
            using var doc = JsonDocument.Parse(input.Template.Variables);
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
            var Deployment = new DeploymentOrchestrationInput()
            {
                DeploymentName = "ResourceIteration",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/ResourceIteration"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = Deployment.Validate();
            Assert.True(r);
            Assert.True(Deployment.Template.Resources.ContainsKey("storagecopy"));
            var copy = Deployment.Template.Resources["storagecopy"];
            Assert.NotNull(copy.Copy);
            Assert.Equal("storagecopy", copy.Copy.Name);
            Assert.Equal(3, copy.Copy.Count);
        }

        [Fact(DisplayName = "PropertyIteration")]
        public void PropertyIteration()
        {
            var Deployment = new DeploymentOrchestrationInput()
            {
                DeploymentName = "PropertyIteration",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/PropertyIteration"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = Deployment.Validate();
            Assert.True(r);
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
        }

        [Fact(DisplayName = "ChildResource")]
        public void ChildResource()
        {
            var Deployment = new DeploymentOrchestrationInput()
            {
                DeploymentName = "ChildResource",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("ChildResource"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = Deployment.Validate();
            Assert.True(r);
            Assert.Equal(2, Deployment.Template.Resources.Count);
            Assert.True(Deployment.Template.Resources.TryGetValue("VNet1", out Resource v));
            Assert.True(Deployment.Template.Resources.TryGetValue("Subnet1", out Resource s));
            Assert.Equal("Microsoft.Network/virtualNetworks", v.FullType);
            Assert.Equal("Microsoft.Network/virtualNetworks/subnets", s.FullType);
        }

        [Fact(DisplayName = "NestTemplate")]
        public void NestTemplate()
        {
            var Deployment = new DeploymentOrchestrationInput()
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
            };
            var (r, m) = Deployment.Validate();
            Assert.True(r);
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
        }

        [Fact(DisplayName = "DoubleNestTemplate")]
        public void DoubleNestTemplate()
        {
            var Deployment = new DeploymentOrchestrationInput()
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
            };
            var (r, m) = Deployment.Validate();

            Assert.True(r);

            Assert.Single(Deployment.Deployments);
            Assert.Equal(2, Deployment.EnumerateDeployments().Count());

            var d1 = Deployment.Deployments["nestedTemplate1"];
            Assert.Equal("2017-05-10", d1.ApiVersion);
            Assert.Equal(Deployment.RootId, d1.RootId);
            Assert.NotNull(d1.DeploymentId);

            Assert.NotNull(d1.Template);
            var t = d1.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.First();
            Assert.Equal("nestedTemplate2", res.FullName);
            Assert.Equal("Microsoft.Resources/deployments", res.FullType);

            Assert.Single(d1.Deployments);
            var d2 = d1.Deployments["nestedTemplate2"];
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
            var Deployment = new DeploymentOrchestrationInput()
            {
                DeploymentName = "ExpressionEvaluationScopeInner",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("ExpressionsInNestedTemplates-inner"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = Deployment.Validate();
            Assert.True(r);
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
            var Deployment = new DeploymentOrchestrationInput()
            {
                DeploymentName = "ExpressionEvaluationScopeOuter",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("ExpressionsInNestedTemplates-outer"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = Deployment.Validate();
            Assert.True(r);
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