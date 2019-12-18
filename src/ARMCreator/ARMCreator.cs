using System;
using System.Text.Json;

namespace maskx.OrchestrationCreator
{
    public class ARMCreator
    {
        public Type Compile(string template)
        {
            using var jsonDoc = JsonDocument.Parse(template);
            var root = jsonDoc.RootElement;
            var resources = root.GetProperty("resources");
            for (int i = 0; i < resources.GetArrayLength(); i++)
            {
                var resource = resources[i];
                var dependsOn = resource.GetProperty("dependsOn");
            }
            return null;
        }
    }
}