using maskx.OrchestrationCreator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ARMCreatorTest
{
    public class TestHelper
    {
        public static string GetFunctionInputContent(string filename)
        {
            string s = Path.Combine(AppContext.BaseDirectory, "Inputs\\functions", $"{filename}.json");
            return File.ReadAllText(s);
        }
        public static string GetNodeStringValue(string filename, string path)
        {
            var templatString = TestHelper.GetFunctionInputContent(filename);
            return new JsonValue(templatString).GetNodeStringValue(path);
        }
        public static void FunctionTest(string filename, Dictionary<string, string> result)
        {
            var templatString = TestHelper.GetFunctionInputContent(filename);
            ARMOrchestration orchestration = new ARMOrchestration();
            var outputString = orchestration.RunTask(null, (templatString, string.Empty)).Result;
            using var templateDoc = JsonDocument.Parse(templatString);
            using var outputDoc = JsonDocument.Parse(outputString);
            var outputRoot = outputDoc.RootElement;
            Assert.True(templateDoc.RootElement.TryGetProperty("outputs", out JsonElement outputDefineElement));

            List<string> child = new List<string>();
            foreach (var item in outputDefineElement.EnumerateObject())
            {
                Assert.True(outputRoot.TryGetProperty(item.Name, out JsonElement v));
                Assert.Equal(result[item.Name], v.GetRawText());
            }
        }
    }
}