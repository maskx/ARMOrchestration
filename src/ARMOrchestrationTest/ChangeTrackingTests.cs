using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                DeploymentName = "VariableIteration",
                Template = TestHelper.GetJsonFileContent("ValidateTemplateTests/Template/Empty"),
                ServiceProvider = fixture.ServiceProvider
            };
            Assert.Empty(input.Template.Resources);

            // add new resource
            input.Template.Resources.Add(new Resource()
            {
                Name = "Name1",
                Properties = "{}"
            });

            Assert.Single(input.Template.Resources);

            var r = input.Template.Resources.First();
            Assert.Equal("Name1", r.Name);

            // change resource's name property
            r.Name = "Name1-Changed";

            using var doc1 = JsonDocument.Parse(input.Template.RawString);
            var resourcesE = doc1.RootElement.GetProperty("resources");
            Assert.Single(resourcesE.EnumerateArray());
            var resE = resourcesE.EnumerateArray().First();
            Assert.True(resE.TryGetProperty("name", out JsonElement nameE));
            Assert.Equal("Name1-Changed", nameE.GetString());

            // change resource's properties property
            r.Properties = "{\"p1\":123'}";
            using var doc2 = JsonDocument.Parse(input.Template.RawString);
            resourcesE = doc2.RootElement.GetProperty("resources");
            Assert.Single(resourcesE.EnumerateArray());
            var res2 = resourcesE.EnumerateArray().First();
            Assert.True(res2.TryGetProperty("properties", out JsonElement properties));
            Assert.Equal("{\"p1\":123'}", properties.GetString());
        }
    }
}