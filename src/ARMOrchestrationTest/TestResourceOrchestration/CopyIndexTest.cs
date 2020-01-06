using ARMCreatorTest;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;
using Newtonsoft.Json.Linq;
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
        private ARMOrchestartionFixture fixture;

        public CopyIndexTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "ResourceIteration")]
        public void ResourceIteration()
        {
            TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "CopyIndex/ResourceIteration");
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

            var instance = TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
                "CopyIndex/VariableIteration", (instance, args) =>
                {
                    return !args.IsSubOrchestration && instance.InstanceId == args.InstanceId;
                }, (instance, cxt) =>
                {
                    var outputString = TestHelper.DataConverter.Deserialize<TaskResult>(cxt.Result).Content;
                    using var templateDoc = JsonDocument.Parse(TestHelper.GetTemplateContent("CopyIndex/VariableIteration"));
                    using var outputDoc = JsonDocument.Parse(outputString);
                    var outputRoot = outputDoc.RootElement;
                    if (templateDoc.RootElement.TryGetProperty("outputs", out JsonElement outputDefineElement))
                    {
                        List<string> child = new List<string>();
                        foreach (var item in outputDefineElement.EnumerateObject())
                        {
                            Assert.True(outputRoot.TryGetProperty(item.Name, out JsonElement v), $"cannot find {item.Name} in output");
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
            var str = TestHelper.GetTemplateContent("CopyIndex/PropertyIteration");
            JObject jObject = JObject.Parse(str);
            var properties = jObject["resources"][0]["properties"] as JObject;
            foreach (var p in properties)
            {
                var b = p.Value as JObject;
                if (b.TryGetValue("copy", out JToken c))
                {
                    b.Add("qqq", 123);
                    b.Remove("copy");
                }
            }
            var d = "";
            //TestHelper.OrchestrationTest(fixture.OrchestrationWorker,
            //    "CopyIndex/PropertyIteration");
        }
    }
}