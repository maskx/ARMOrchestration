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

        public Dictionary<string, Resource> Resources { get; set; }

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
    }
}