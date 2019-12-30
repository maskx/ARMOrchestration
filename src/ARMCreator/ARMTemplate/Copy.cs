using System.Text.Json;

namespace maskx.OrchestrationCreator.ARMTemplate
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances
    /// </summary>
    public class Copy
    {
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
        public string Mode { get; set; }

        /// <summary>
        /// number-to-deploy-serially
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// values-for-the-property-or-variable
        /// </summary>
        public string Input { get; set; }

        public static Copy Parse(string str)
        {
            var c = new Copy();
            using JsonDocument json = JsonDocument.Parse(str);
            var root = json.RootElement;
            if (root.TryGetProperty("name", out JsonElement name))
            {
                c.Name = name.GetString();
            }
            if (root.TryGetProperty("count", out JsonElement count))
            {
                c.Count = count.GetInt32();
            }
            if (root.TryGetProperty("mode", out JsonElement mode))
            {
                c.Mode = mode.GetString();
            }
            if (root.TryGetProperty("batchSize", out JsonElement batchSize))
            {
                c.BatchSize = batchSize.GetInt32();
            }
            if (root.TryGetProperty("input", out JsonElement input))
            {
                c.Input = input.GetString();
            }
            return c;
        }
    }
}