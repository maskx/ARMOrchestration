using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances
    /// </summary>
    public class Copy
    {
        public const string ServiceType = "copy";
        public const string SerialMode = "serial";
        public const string ParallelMode = "parallel";

        public string Id { get; set; }

        /// <summary>
        /// name-of-loop
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// number-of-iterations
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// "serial" <or> "parallel"
        /// </summary>
        public string Mode { get; set; } = ParallelMode;

        /// <summary>
        /// number-to-deploy-serially
        /// </summary>
        public int BatchSize { get; set; } = 0;

        /// <summary>
        /// values-for-the-property-or-variable
        /// </summary>
        public string Input { get; set; }
        public static Copy Parse(string rawString, Dictionary<string, object> context, ARMFunctions functions, IInfrastructure infrastructure)
        {
            using var doc = JsonDocument.Parse(rawString);
            return Parse(doc.RootElement, context, functions, infrastructure);
        }
        public static Copy Parse(JsonElement root, Dictionary<string, object> context, ARMFunctions functions, IInfrastructure infrastructure)
        {
            var copy = new Copy();
            var deployContext = context[ContextKeys.ARM_CONTEXT] as DeploymentContext;
            if (root.TryGetProperty("name", out JsonElement name))
                copy.Name = name.GetString();
            if (root.TryGetProperty("count", out JsonElement count))
            {
                if (count.ValueKind == JsonValueKind.Number)
                    copy.Count = count.GetInt32();
                else if (count.ValueKind == JsonValueKind.String)
                    copy.Count = (int)functions.Evaluate(count.GetString(), context);
                else
                    throw new Exception("the value of count property should be Number in copy node");
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#valid-uses-1
                if (context.ContainsKey(ContextKeys.DEPENDSON))
                    throw new Exception("You can't use the reference function to set the value of the count property in a copy loop.");
            }
            else
                throw new Exception("not find count in copy node");
            if (root.TryGetProperty("mode", out JsonElement mode))
            {
                copy.Mode = mode.GetString().ToLower();
            }
            if (root.TryGetProperty("batchSize", out JsonElement batchSize))
            {
                if (batchSize.ValueKind == JsonValueKind.Number)
                    copy.BatchSize = batchSize.GetInt32();
                else if (batchSize.ValueKind == JsonValueKind.String)
                    copy.BatchSize = (int)functions.Evaluate(batchSize.GetString(), context);
            }
            if (root.TryGetProperty("input", out JsonElement input))
            {
                copy.Input = input.GetRawText();
            }
            copy.Id = functions.ResourceId(deployContext, new object[] {
                $"{infrastructure.BuitinServiceTypes.Deployments}/{Copy.ServiceType}",
                deployContext.DeploymentName,
                copy.Name
            });
            return copy;
        }
    }
}