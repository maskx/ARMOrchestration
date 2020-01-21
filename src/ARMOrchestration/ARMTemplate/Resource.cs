using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Orchestrations;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// 部署服务需提供以下API
    /// https://docs.microsoft.com/en-us/rest/api/resources/
    /// </summary>
    public class Resource
    {
        public bool Condition { get; set; } = true;

        public string ApiVersion { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public string Tags { get; set; }

        public string Comments { get; set; }

        /// <summary>
        /// The list can include resources that are conditionally deployed. When a conditional resource isn't deployed, Azure Resource Manager automatically removes it from the required dependencies.
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/define-resource-dependency#dependson
        ///
        /// Depend on resources in a loop
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances#depend-on-resources-in-a-loop
        ///
        ///
        /// You only need to define dependencies for resources that are deployed in the same template.
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/define-resource-dependency
        ///
        /// </summary>
        public List<string> DependsOn { get; set; }

        public string Properties { get; set; }

        public string SKU { get; set; }

        public string Kind { get; set; }

        public string Plan { get; set; }

        public List<Resource> Resources { get; set; } = new List<Resource>();
        public Dictionary<string, string> ExtensionResource { get; set; } = new Dictionary<string, string>();
        public string ResourceGroup { get; set; }

        public string SubscriptionId { get; set; }

        public string ResouceId { get; set; }

        public bool TryGetExtensionResource(string name, out string resource)
        {
            resource = null;
            if (string.IsNullOrEmpty(this.Properties))
                return false;
            using var doc = JsonDocument.Parse(this.Properties);
            var root = doc.RootElement;
            if (root.TryGetProperty(name, out JsonElement r))
            {
                resource = r.GetRawText();
                return true;
            }

            return false;
        }
    }
}