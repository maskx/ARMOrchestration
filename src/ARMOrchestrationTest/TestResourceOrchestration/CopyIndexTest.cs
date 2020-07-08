using ARMCreatorTest;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace ARMOrchestrationTest.TestResourceOrchestration
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ResourceOrchestration")]
    [Trait("ResourceOrchestration", "CopyIndex")]
    public class CopyIndexTest
    {
        private readonly ARMOrchestartionFixture fixture;

        public CopyIndexTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "ResourceIteration")]
        public void ResourceIteration()
        {
            TestHelper.OrchestrationTest(fixture,
                "CopyIndex/ResourceIteration");
        }
        [Fact(DisplayName = "ResourceIteration_BatchSize")]
        public void ResourceIteration_BatchSize()
        {
            TestHelper.OrchestrationTest(fixture,
                "CopyIndex/ResourceIteration_BatchSize");
        }

        [Fact(DisplayName = "VariableIteration")]
        public void VariableIteration()
        {
            Dictionary<string, string> result = new Dictionary<string, string>() {
                { "exampleObject","{\"disks\":[{\"name\":\"myDataDisk1\",\"diskSizeGB\":\"1\",\"diskIndex\":0},{\"name\":\"myDataDisk2\",\"diskSizeGB\":\"1\",\"diskIndex\":1},{\"name\":\"myDataDisk3\",\"diskSizeGB\":\"1\",\"diskIndex\":2},{\"name\":\"myDataDisk4\",\"diskSizeGB\":\"1\",\"diskIndex\":3},{\"name\":\"myDataDisk5\",\"diskSizeGB\":\"1\",\"diskIndex\":4}],\"diskNames\":[\"myDataDisk1\",\"myDataDisk2\",\"myDataDisk3\",\"myDataDisk4\",\"myDataDisk5\"]}"},
                { "exampleArrayOnObject","[{\"name\":\"myDataDisk1\",\"diskSizeGB\":\"1\",\"diskIndex\":0},{\"name\":\"myDataDisk2\",\"diskSizeGB\":\"1\",\"diskIndex\":1},{\"name\":\"myDataDisk3\",\"diskSizeGB\":\"1\",\"diskIndex\":2},{\"name\":\"myDataDisk4\",\"diskSizeGB\":\"1\",\"diskIndex\":3},{\"name\":\"myDataDisk5\",\"diskSizeGB\":\"1\",\"diskIndex\":4}]"},
                { "exampleObjectArray","[{\"name\":\"myDataDisk1\",\"diskSizeGB\":\"1\",\"diskIndex\":0},{\"name\":\"myDataDisk2\",\"diskSizeGB\":\"1\",\"diskIndex\":1},{\"name\":\"myDataDisk3\",\"diskSizeGB\":\"1\",\"diskIndex\":2},{\"name\":\"myDataDisk4\",\"diskSizeGB\":\"1\",\"diskIndex\":3},{\"name\":\"myDataDisk5\",\"diskSizeGB\":\"1\",\"diskIndex\":4}]"},
                { "exampleStringArray","[\"myDataDisk1\",\"myDataDisk2\",\"myDataDisk3\",\"myDataDisk4\",\"myDataDisk5\"]"},
                { "exampleIntegerArray","[0,1,2,3,4]"}
            };

            var instance = TestHelper.OrchestrationTest(fixture,
                "CopyIndex/VariableIteration",
                (instance, args) =>
                {
                    return !args.IsSubOrchestration
                    && instance.InstanceId == args.InstanceId;
                }, (instance, cxt) =>
                {
                    var outputString = TestHelper.DataConverter.Deserialize<TaskResult>(cxt.Result).Content;
                    using var templateDoc = JsonDocument.Parse(TestHelper.GetTemplateContent("CopyIndex/VariableIteration"));
                    using var outputDoc = JsonDocument.Parse(outputString);
                    var outputRoot = outputDoc.RootElement.GetProperty("properties").GetProperty("outputs");
                    if (templateDoc.RootElement.TryGetProperty("outputs", out JsonElement outputDefineElement))
                    {
                        List<string> child = new List<string>();
                        foreach (var item in outputDefineElement.EnumerateObject())
                        {
                            Assert.True(outputRoot.TryGetProperty(item.Name, out JsonElement o), $"cannot find {item.Name} in output");
                            o.TryGetProperty("value", out JsonElement v);
                            if (v.ValueKind == JsonValueKind.String)
                                Assert.True(result[item.Name] == v.GetString(), $"{item.Name} test fail, Expected:{result[item.Name]},Actual:{v.GetString()}");
                            else
                                Assert.True(result[item.Name] == v.GetRawText(), $"{item.Name} test fail, Expected:{result[item.Name]},Actual:{v.GetRawText()}");
                        }
                    }
                });
        }

        [Fact(DisplayName = "PropertyIteration")]
        public void PropertyIteration()
        {
            TestHelper.OrchestrationTest(fixture,
                "CopyIndex/PropertyIteration");
        }

        [Fact(DisplayName = "CopyIndexOutput")]
        public void CopyIndexOutput()
        {
            var instance = TestHelper.OrchestrationTest(fixture,
                  "CopyIndex/output");
            var r = this.fixture.ARMOrchestrationClient.GetResourceListAsync(instance.InstanceId).Result[0];
            var result = JsonDocument.Parse(r.Result).RootElement;
            var storageEndpoints = result.GetProperty("properties").GetProperty("outputs").GetProperty("storageEndpoints");
            Assert.True(storageEndpoints.TryGetProperty("value", out JsonElement value));
            Assert.Equal(JsonValueKind.Array, value.ValueKind);
            Assert.Equal(3, value.GetArrayLength());
            bool has0 = false;
            bool has1 = false;
            bool has2 = false;
            foreach (var item in value.EnumerateArray())
            {
                var s = item.GetString();
                Assert.EndsWith("stroageName", s);
                Assert.True(int.TryParse(s[0].ToString(), out int num));
                if (num == 0) has0 = true;
                if (num == 1) has1 = true;
                if (num == 2) has2 = true;
            }
            Assert.True(has0);
            Assert.True(has1);
            Assert.True(has2);
        }
    }
}