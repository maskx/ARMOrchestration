using DurableTask.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator
{
    public class CreateOrUpdateOrchestration : TaskOrchestration<string, CreateOrUpdateInput>
    {
        public override async Task<string> RunTask(OrchestrationContext context, CreateOrUpdateInput input)
        {
            using var jsonDoc = JsonDocument.Parse(input.Resource);
            var root = jsonDoc.RootElement;
            if (!CheckCondition(root))
                return string.Empty;

            //if (string.IsNullOrEmpty(condition))
            //{
            //}
            //for (int i = 0; i < resources.GetArrayLength(); i++)
            //{
            //    var resource = resources[i];
            //    var dependsOn = resource.GetProperty("dependsOn");
            //}
            return string.Empty;
        }

        private bool CheckCondition(JsonElement ele)
        {
            if (ele.TryGetProperty("condition", out JsonElement conditionNode))
            {
                if (conditionNode.ValueKind == JsonValueKind.False)
                    return false;
                if (conditionNode.ValueKind == JsonValueKind.String)
                {
                    string conditionString = conditionNode.GetString();
                    if (string.IsNullOrEmpty(conditionString))
                        return true;
                    var rtv = RunFunction(conditionString).ToString();
                    if (bool.TryParse(rtv, out bool v))
                    {
                        return v;
                    }
                }
            }
            return true;
        }

        private object RunFunction(string content)
        {
            if (content.StartsWith("[") && content.EndsWith("]") && !content.StartsWith("[["))
            {
                string functionString = content.TrimStart('[').TrimEnd(']');
                return ARMFunctions.Evaluate(functionString, null);
            }
            return content;
        }
    }
}