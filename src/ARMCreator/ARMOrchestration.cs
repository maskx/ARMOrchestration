using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator
{
    public class ARMOrchestration : TaskOrchestration<string, (string Template, string Parameters)>
    {
        public override async Task<string> RunTask(OrchestrationContext context, (string Template, string Parameters) input)
        {
            List<Task> tasks = new List<Task>();
            using var jsonDoc = JsonDocument.Parse(input.Template);
            var root = jsonDoc.RootElement;
            string parameterDefineString = string.Empty;
            string variableDefineString = string.Empty;
            if (root.TryGetProperty("parameters", out JsonElement parameterDefineElement))
            {
                parameterDefineString = parameterDefineElement.GetRawText();
            }
            if (root.TryGetProperty("variables", out JsonElement variableDefineElement))
            {
                variableDefineString = variableDefineElement.GetRawText();
            }
            var resources = root.GetProperty("resources");
            for (int i = 0; i < resources.GetArrayLength(); i++)
            {
                var resource = resources[i];
                var p = new CreateOrUpdateInput()
                {
                    Resource = resource.GetRawText(),
                    Parameters = input.Parameters,
                    Variable = variableDefineString,
                    ParameterDefine = parameterDefineString
                };
                tasks.Add(context.CreateSubOrchestrationInstance<string>(typeof(CreateOrUpdateOrchestration), p));
            }
            Task.WaitAll(tasks.ToArray());
            if (root.TryGetProperty("outputs", out JsonElement outputDefineElement))
            {
                List<string> child = new List<string>();
                foreach (var item in outputDefineElement.EnumerateObject())
                {
                    var type = item.Value.GetProperty("type").GetString();
                    var value = item.Value.GetProperty("value").GetString();
                    var v = ARMFunctions.Run(value, new Dictionary<string, object>() {
                        { "parametersDefine",parameterDefineString},
                        { "variabledefine",variableDefineString},
                        { "parameters",input.Parameters}
                    });
                    var t = type.ToLower();
                    if ("string" == t || "bool" == t)
                        child.Add($"\"{item.Name}\":\"{v}\"");
                    else if ("object" == t || "array" == t)
                        child.Add($"\"{item.Name}\":{(v as JsonValue).RawString}");
                    else
                        child.Add($"\"{item.Name}\":{v}");
                }
                return $"{{{string.Join(",", child)}}}";
            }
            return string.Empty;
        }
    }
}