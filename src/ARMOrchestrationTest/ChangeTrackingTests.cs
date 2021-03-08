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
            var input = new maskx.ARMOrchestration.Deployment()
            {
                Name = "VariableIteration",
                Template = TestHelper.GetJsonFileContent("Templates/NestTemplate/NestTemplate"),
                ServiceProvider = fixture.ServiceProvider,
                SubscriptionId = Guid.NewGuid().ToString("N"),
                ResourceGroup = "ChangeTrackingTest",
                DeploymentId = Guid.NewGuid().ToString("N")
            };
            Assert.Single(input.EnumerateResource());
            Assert.Single(input.EnumerateDeployments());

            var nestTemplate = input.GetFirstResource("nestedTemplate1");

            #region add new resource
            //input.Template.Resources.Add(new Resource()
            //{
            //    Type = "isv.rp/st",
            //    Name = "Name1",
            //    RawProperties = "{}"
            //});

            Assert.Equal(2, input.Template.Resources.Count);
            Assert.Equal(2, input.EnumerateResource().Count());

            var r = input.GetFirstResource("Name1");
            Assert.Equal("Name1", r.Name);
            #endregion

            #region change resource's name property

            nestTemplate.Name = "nestTemplate1-Changed";

            Assert.Null(input.GetFirstResource("nestTemplate1"));
            Assert.NotNull(input.GetFirstResource("nestTemplate1-Changed"));

            #endregion

            #region change resource's properties property
            var p = nestTemplate.RawProperties;
            nestTemplate.RawProperties = "{\"p1\":123}";
            Assert.Equal("{\"p1\":123}", nestTemplate.RawProperties);
            nestTemplate.RawProperties = p;
            Assert.Equal(p, nestTemplate.RawProperties);
            #endregion

            #region modify DependsOn

            Assert.Empty(r.DependsOn);
            //input.Template.Resources.Add(new Resource()
            //{
            //    Type = "isv.rp/st",
            //    Name = "Name2",
            //    RawProperties = "{}",
            //});
            var r2 = input.Template.Resources["Name2"];
            Assert.Empty(r2.DependsOn);
            r2.DependsOn.Add("Name1", input);
            Assert.Single(r2.DependsOn);
            r2.DependsOn.Remove("Name1");
            Assert.Empty(r2.DependsOn);

            r2.RawProperties = "{\"property1\":\"[reference('Name1-Changed',true).name]\"}";
            Assert.Single(r2.DependsOn);

            using var docr2 = JsonDocument.Parse(r2.ToString());
            Assert.True(docr2.RootElement.TryGetProperty("dependsOn", out JsonElement dependsOnE));
            Assert.Equal(JsonValueKind.Array, dependsOnE.ValueKind);
            Assert.Single(dependsOnE.EnumerateArray());
            Assert.Equal("isv.rp/st/Name1-Changed", dependsOnE.EnumerateArray().First().GetString());

            #endregion

            #region Zones
            Assert.Empty(r.Zones);
            r.Zones.Add("zone1");
            Assert.Single(r.Zones);
            using var docZone = JsonDocument.Parse(r.ToString());
            Assert.True(docZone.RootElement.TryGetProperty("zones", out JsonElement zonesE));
            Assert.Equal(JsonValueKind.Array, zonesE.ValueKind);
            Assert.Single(zonesE.EnumerateArray());
            Assert.Equal("zone1", zonesE.EnumerateArray().First().GetString());
            r.Zones.Remove("zone1");
            Assert.Empty(r.Zones);
            using var doczoneEmpty = JsonDocument.Parse(r.ToString());
            Assert.True(doczoneEmpty.RootElement.TryGetProperty("zones", out JsonElement zonesEmpty));
            Assert.Equal(JsonValueKind.Array, zonesEmpty.ValueKind);
            Assert.Empty(zonesEmpty.EnumerateArray());
            #endregion
        }
    }
}