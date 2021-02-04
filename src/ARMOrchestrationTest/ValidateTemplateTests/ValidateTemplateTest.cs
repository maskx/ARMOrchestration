using maskx.ARMOrchestration;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
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
            var r = new Deployment()
            {
                Name = "EmptyTemplate",
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
            var input = new Deployment()
            {
                Name = "NoSchema",
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
            var input = new Deployment()
            {
                Name = "NoContentVersion",
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
            var input = new Deployment()
            {
                Name = "NoResources",
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
            var input = new Deployment()
            {
                Name = "VariableIteration",
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/VariableIteration"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = input.Validate();
            Assert.True(r);
            using var doc = JsonDocument.Parse(input.Template.Variables.ToString());
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
            var Deployment = new Deployment()
            {
                Name = "ResourceIteration",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/ResourceIteration"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = Deployment.Validate();
            Assert.True(r);
            Assert.True(Deployment.Template.Resources.TryGetValue("storagecopy",out Resource _));
            var copy = Deployment.Template.Resources["storagecopy"];
            Assert.NotNull(copy.Copy);
            Assert.Equal("storagecopy", copy.Copy.Name);
            Assert.Equal(3, copy.Copy.Count);
        }

        [Fact(DisplayName = "PropertyIteration")]
        public void PropertyIteration()
        {
            var Deployment = new Deployment()
            {
                Name = "PropertyIteration",
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
            var Deployment = new Deployment()
            {
                Name = "ChildResource",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("ChildResource"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = Deployment.Validate();
            Assert.True(r);
            Assert.Single(Deployment.Template.Resources);
            Assert.True(Deployment.Template.Resources.TryGetValue("VNet1", out Resource v));
            Assert.Single(v.Resources);
            Assert.True(v.Resources.TryGetValue("Subnet1", out Resource s));
            Assert.Equal("Microsoft.Network/virtualNetworks", v.Type);
            Assert.Equal("subnets", s.Type);
            Assert.Equal("Microsoft.Network/virtualNetworks/subnets", s.FullType);
            Assert.Equal("Subnet1", s.Name);
            Assert.Equal("VNet1/Subnet1", s.FullName);
            Assert.Equal("Microsoft.Network/virtualNetworks/VNet1/subnets/Subnet1", s.NameWithServiceType);
            Assert.Equal($"/subscription/{TestHelper.SubscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.Network/virtualNetworks/VNet1/subnets/Subnet1", s.ResourceId, true);
            Assert.Equal(2, Deployment.EnumerateResource(true).Count());
        }

        [Fact(DisplayName = "NestTemplate")]
        public void NestTemplate()
        {
            var Deployment = new Deployment()
            {
                Name = "NestTemplate",
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
            Assert.Single(Deployment.EnumerateDeployments());
            var d = Deployment.EnumerateDeployments().First();
            Assert.Equal("nestedTemplate1", d.Name);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.Equal(Deployment.RootId, d.RootId);
            Assert.NotNull(d.DeploymentId);

            Assert.NotNull(d.Template);
            var t = d.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.First();
            Assert.Equal("storageAccount1", res.Name);
            Assert.Equal("Microsoft.Storage/storageAccounts", res.Type);
        }

        [Fact(DisplayName = "DoubleNestTemplate")]
        public void DoubleNestTemplate()
        {
            var Deployment = new Deployment()
            {
                Name = "DoubleNestTemplate",
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

            Assert.Equal(2, Deployment.EnumerateDeployments().Count());

            var d1 = Deployment.EnumerateDeployments().First((input) => input.Name == "nestedTemplate1");
            Assert.Equal("2017-05-10", d1.ApiVersion);
            Assert.Equal(Deployment.RootId, d1.RootId);
            Assert.NotNull(d1.DeploymentId);

            Assert.NotNull(d1.Template);
            var t = d1.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.First();
            Assert.Equal("nestedTemplate2", res.Name);
            Assert.Equal("Microsoft.Resources/deployments", res.Type);

            Assert.Single(d1.EnumerateDeployments());
            var d2 = d1.EnumerateDeployments().First((input) => input.Name == "nestedTemplate2");
            Assert.Equal("2017-05-10", d2.ApiVersion);
            Assert.Equal(Deployment.RootId, d2.RootId);
            Assert.NotNull(d2.DeploymentId);
            Assert.Empty(d2.EnumerateDeployments());

            Assert.NotNull(d2.Template);
            var t2 = d2.Template;
            Assert.Single(t2.Resources);
            var res2 = t2.Resources.First();
            Assert.Equal("storageAccount1", res2.Name);
            Assert.Equal("Microsoft.Storage/storageAccounts", res2.Type);
        }

        [Fact(DisplayName = "ExpressionEvaluationScopeInner")]
        public void ExpressionEvaluationScopeInner()
        {
            var Deployment = new Deployment()
            {
                Name = "ExpressionEvaluationScopeInner",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("ExpressionsInNestedTemplates-inner"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = Deployment.Validate();
            Assert.True(r);
            Assert.Single(Deployment.EnumerateDeployments());
            var d = Deployment.EnumerateDeployments().First();
            Assert.Equal("nestedTemplate1", d.Name);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.NotNull(d.Template);
            var t = d.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.First();
            Assert.Equal("from nested template", res.Name);
            var s = Deployment.Template.ToString();
            using var doc1 = JsonDocument.Parse(s);
            var root1 = doc1.RootElement;
        }

        [Fact(DisplayName = "ExpressionEvaluationScopeOuter")]
        public void ExpressionEvaluationScopeOuter()
        {
            var Deployment = new Deployment()
            {
                Name = "ExpressionEvaluationScopeOuter",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = GetTemplate("ExpressionsInNestedTemplates-outer"),
                ServiceProvider = fixture.ServiceProvider
            };
            var (r, m) = Deployment.Validate();
            Assert.True(r);
            Assert.Single(Deployment.EnumerateDeployments());
            var d = Deployment.EnumerateDeployments().First();
            Assert.Equal("nestedTemplate1", d.Name);
            Assert.Equal("2017-05-10", d.ApiVersion);
            Assert.NotNull(d.Template);
            var t = d.Template;
            Assert.Single(t.Resources);
            var res = t.Resources.First();
            Assert.Equal("from parent template", res.Name);
            var s = Deployment.Template.ToString();
            using var doc1 = JsonDocument.Parse(s);
            var root1 = doc1.RootElement;
        }

        [Fact(DisplayName = "ModifyCopyResource")]
        public void ModifyCopyResource()
        {
            var Deployment = new Deployment()
            {
                Name = "ResourceIteration",
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/ResourceIteration"),
                ServiceProvider = fixture.ServiceProvider
            };
            Assert.Empty(Deployment.Template.ChangedCopyResoures);
            var copy=Deployment.Template.Resources["storagecopy"];
            var r=copy.Copy.EnumerateResource().First();
            Assert.Equal(0, r.CopyIndex);
            r.Comments = "123";
            Assert.Single(Deployment.Template.ChangedCopyResoures);
            var rr = copy.Copy.EnumerateResource().First();
            Assert.Equal("123", rr.Comments);
            var str=TestHelper.DataConverter.Serialize(Deployment.Template);
            Assert.Contains("123", str);
          
            var c1 = Deployment.Template.Resources["storagecopy"];
            var r1 = c1.Copy.EnumerateResource().First();
            Assert.Equal(0, r1.CopyIndex);
            Assert.Equal("123", r1.Comments);
        }
    }
}