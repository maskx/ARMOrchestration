using ARMCreatorTest;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Orchestrations;
using System.Text.Json;
using Xunit;

namespace ARMOrchestrationTest.ValidateTemplateTests
{
    [Trait("c", "ValidateTemplate")]
    public class ValidateTemplateTest
    {
        private string GetTemplate(string filename)
        {
            return TestHelper.GetJsonFileContent("ValidateTemplateTests/Template/" + filename);
        }

        [Fact(DisplayName = "EmptyTemplate")]
        public void EmptyTemplate()
        {
            var r = Helper.ValidateTemplate(new TemplateOrchestrationInput()
            {
                Template = GetTemplate("Empty")
            });
            Assert.True(r.Result);
        }

        [Fact(DisplayName = "NoSchema")]
        public void NoSchema()
        {
            var r = Helper.ValidateTemplate(new TemplateOrchestrationInput()
            {
                Template = GetTemplate("NoSchema")
            });
            Assert.False(r.Result);
            Assert.Equal("not find $schema in template", r.Message);
        }

        [Fact(DisplayName = "NoContentVersion")]
        public void NoContentVersion()
        {
            var r = Helper.ValidateTemplate(new TemplateOrchestrationInput()
            {
                Template = GetTemplate("NoContentVersion")
            });
            Assert.False(r.Result);
            Assert.Equal("not find contentVersion in template", r.Message);
        }

        [Fact(DisplayName = "NoResources")]
        public void NoResources()
        {
            var r = Helper.ValidateTemplate(new TemplateOrchestrationInput()
            {
                Template = GetTemplate("NoResources")
            });
            Assert.False(r.Result);
            Assert.Equal("not find resources in template", r.Message);
        }

        [Fact(DisplayName = "VariableIteration")]
        public void VariableIteration()
        {
            var r = Helper.ValidateTemplate(new TemplateOrchestrationInput()
            {
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/VariableIteration")
            });
            Assert.True(r.Result);
            using var doc = JsonDocument.Parse(r.Template.Variables);
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
            var r = Helper.ValidateTemplate(new TemplateOrchestrationInput()
            {
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/ResourceIteration")
            });
            Assert.True(r.Result);
            Assert.Single(r.Template.Copys);
            Assert.True(r.Template.Copys.ContainsKey("storagecopy"));
            Assert.Equal(3, r.Template.Copys["storagecopy"].Count);
            var resource = r.Template.Copys["storagecopy"][0];
            Assert.Equal("0storage", resource.Name);
            Assert.Equal("Microsoft.Storage/storageAccounts", resource.Type);
        }

        [Fact(DisplayName = "PropertyIteration")]
        public void PropertyIteration()
        {
            var r = Helper.ValidateTemplate(new TemplateOrchestrationInput()
            {
                Template = TestHelper.GetJsonFileContent("Templates/CopyIndex/PropertyIteration")
            });
            Assert.True(r.Result);
            Assert.Single(r.Template.Resources);
            var resource = r.Template.Resources[0];
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
            var r = Helper.ValidateTemplate(new TemplateOrchestrationInput()
            {
                Template = GetTemplate("ChildResource")
            });
            Assert.True(r.Result);
            Assert.Single(r.Template.Resources);
            var childResources = r.Template.Resources[0].Resources;
            Assert.Single(childResources);
            var childres = childResources[0];
            Assert.Equal("subnets", childres.Type);
        }
    }
}