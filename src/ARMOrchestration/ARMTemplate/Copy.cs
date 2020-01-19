using maskx.ARMOrchestration.Orchestrations;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances
    /// </summary>
    public class Copy
    {
        public const string ServiceType = "Copy";
        public const string SerialMode = "serial";
        public const string ParallelMode = "parallel";

        public Dictionary<string, Resource> Resources { get; set; } = new Dictionary<string, Resource>();

        public static (bool Result, string Message, Copy Copy) Parse(string jsonString, Dictionary<string, object> context)
        {
            var copy = new Copy();
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;
            if (root.TryGetProperty("name", out JsonElement name))
                copy.Name = name.GetString();
            else
                return (false, "not find name in copy node", null);
            if (root.TryGetProperty("count", out JsonElement count))
            {
                if (count.ValueKind == JsonValueKind.Number)
                    copy.Count = count.GetInt32();
                else if (count.ValueKind == JsonValueKind.String)
                    copy.Count = (int)ARMFunctions.Evaluate(count.GetString(), context);
                else
                    return (false, "the value of count property should be Number in copy node", null);
            }
            else
                return (false, "not find count in copy node", null);
            if (root.TryGetProperty("mode", out JsonElement mode))
            {
                copy.Mode = mode.GetString().ToLower();
            }
            if (root.TryGetProperty("batchSize", out JsonElement batchSize))
            {
                if (batchSize.ValueKind == JsonValueKind.Number)
                    copy.BatchSize = batchSize.GetInt32();
                else if (batchSize.ValueKind == JsonValueKind.String)
                    copy.BatchSize = (int)ARMFunctions.Evaluate(batchSize.GetString(), context);
            }
            if (root.TryGetProperty("input", out JsonElement input))
            {
                copy.Input = input.GetRawText();
            }
            return (true, string.Empty, copy);
        }

        public string GetId(string deploymentId)
        {
            return $"deployment/{deploymentId}/copy/{this.Name}";
        }

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
    }
}