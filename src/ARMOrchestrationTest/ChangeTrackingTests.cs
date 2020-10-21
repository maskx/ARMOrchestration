using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace ARMOrchestrationTest
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ChangeTrackingTests")]
    public class ChangeTrackingTests
    {
        private readonly ARMOrchestartionFixture fixture;

        public ChangeTrackingTests(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "Empty")]
        public void Empty()
        {
            var input = new DeploymentOrchestrationInput()
            {
                Name = "VariableIteration",
                Template = TestHelper.GetJsonFileContent("ValidateTemplateTests/Template/Empty"),
                ServiceProvider = fixture.ServiceProvider
            };
            Assert.Empty(input.Template.Resources);

            #region add new resource
            input.Template.Resources.Add(new Resource()
            {
                Type = "isv.rp/st",
                Name = "Name1",
                RawProperties = "{}"
            });

            Assert.Single(input.Template.Resources);

            var r = input.Template.Resources.First();
            Assert.Equal("Name1", r.Name);
            #endregion

            #region change resource's name property
            r.Name = "Name1-Changed";

            using var doc1 = JsonDocument.Parse(input.Template.RawString);
            var resourcesE = doc1.RootElement.GetProperty("resources");
            Assert.Single(resourcesE.EnumerateArray());
            var resE = resourcesE.EnumerateArray().First();
            Assert.True(resE.TryGetProperty("name", out JsonElement nameE));
            Assert.Equal("Name1-Changed", nameE.GetString());
            #endregion

            #region change resource's properties property
            r.RawProperties = "{\"p1\":123}";
            Assert.Equal("{\"p1\":123}", r.Properties);
            using var doc2 = JsonDocument.Parse(input.Template.RawString);
            resourcesE = doc2.RootElement.GetProperty("resources");
            Assert.Single(resourcesE.EnumerateArray());
            var res2 = resourcesE.EnumerateArray().First();
            Assert.True(res2.TryGetProperty("properties", out JsonElement properties));
            Assert.Equal("{\"p1\":123}", properties.GetRawText());
            #endregion

            #region modify DependsOn

            Assert.Empty(r.DependsOn);
            input.Template.Resources.Add(new Resource()
            {
                Type = "isv.rp/st",
                Name = "Name2",
                RawProperties = "{}",
            });
            var r2 = input.Template.Resources["Name2"];
            Assert.Empty(r2.DependsOn);
            Assert.Throws<Exception>(() =>
           {
               r2.DependsOn.Add("Name1", input);
           });
            r2.DependsOn.Add("Name1-Changed", input);
            Assert.Single(r2.DependsOn);
            r2.DependsOn.Remove("Name1-Changed");
            Assert.Empty(r2.DependsOn);

            r2.RawProperties = "{\"property1\":\"[reference('Name1-Changed',true).name]\"}";
            Assert.Single(r2.DependsOn);

            using var docr2 = JsonDocument.Parse(r2.RawString);
            Assert.True(docr2.RootElement.TryGetProperty("dependsOn", out JsonElement dependsOnE));
            Assert.Equal(JsonValueKind.Array, dependsOnE.ValueKind);
            Assert.Single(dependsOnE.EnumerateArray());
            Assert.Equal("isv.rp/st/Name1-Changed", dependsOnE.EnumerateArray().First().GetString());

            #endregion

            #region Zones
            Assert.Empty(r.Zones);
            r.Zones.Add("zone1");
            Assert.Single(r.Zones);
            using var docZone = JsonDocument.Parse(r.RawString);
            Assert.True(docZone.RootElement.TryGetProperty("zones",out JsonElement zonesE));
            Assert.Equal(JsonValueKind.Array, zonesE.ValueKind);
            Assert.Single(zonesE.EnumerateArray());
            Assert.Equal("zone1", zonesE.EnumerateArray().First().GetString());
            r.Zones.Remove("zone1");
            Assert.Empty(r.Zones);
            using var doczoneEmpty = JsonDocument.Parse(r.RawString);
            Assert.True(doczoneEmpty.RootElement.TryGetProperty("zones", out JsonElement zonesEmpty));
            Assert.Equal(JsonValueKind.Array, zonesEmpty.ValueKind);
            Assert.Empty(zonesEmpty.EnumerateArray());
            #endregion
        }
    }
}