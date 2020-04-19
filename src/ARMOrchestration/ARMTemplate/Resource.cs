using System.Collections.Generic;
using System.IO;
using System.Text;
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

        /// <summary>
        /// the type set in template
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// the type of resource
        /// the child resource is parentType/childType
        /// </summary>
        public string FullType { get; set; }

        /// <summary>
        /// the name set in template
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// the name of resource
        /// the child resource is parentName/childName
        /// </summary>
        public string FullName { get; set; }

        public string Location { get; set; }

        public string Comments { get; set; }
        public List<string> Resources { get; set; } = new List<string>();

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
        public List<string> DependsOn { get; set; } = new List<string>();

        public string Properties { get; set; }

        public string SKU { get; set; }

        public string Kind { get; set; }

        public string Plan { get; set; }
        public Dictionary<string, string> ExtensionResource { get; set; } = new Dictionary<string, string>();

        public string ResourceGroup { get; set; }

        public string SubscriptionId { get; set; }
        public string ManagementGroupId { get; set; }

        public string ResouceId { get; set; }
        public string CopyId { get; set; }

        public override string ToString()
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteBoolean("condition", this.Condition);
            writer.WriteString("apiVersion", this.ApiVersion);
            writer.WriteString("type", this.Type);
            writer.WriteString("name", this.Name);
            if (!string.IsNullOrEmpty(this.Location))
            {
                writer.WriteString("location", this.Location);
            }
            if (!string.IsNullOrEmpty(this.Comments))
                writer.WriteString("comments", this.Comments);
            if (this.DependsOn.Count > 0)
            {
                writer.WritePropertyName("dependsOn");
                writer.WriteStartArray();
                foreach (var depend in this.DependsOn)
                {
                    writer.WriteStringValue(depend);
                }
                writer.WriteEndArray();
            }

            if (!string.IsNullOrEmpty(this.Properties))
            {
                writer.WritePropertyName("properties");
                JsonDocument.Parse(this.Properties).RootElement.WriteTo(writer);
            }
            if (!string.IsNullOrEmpty(this.SKU))
            {
                writer.WritePropertyName("sku");
                JsonDocument.Parse(this.SKU).RootElement.WriteTo(writer);
            }
            if (!string.IsNullOrEmpty(this.Kind))
                writer.WriteString("kind", this.Kind);
            if (!string.IsNullOrEmpty(this.Plan))
            {
                writer.WritePropertyName("plan");
                JsonDocument.Parse(this.Plan).RootElement.WriteTo(writer);
            }
            writer.WriteString("resourceGroup", this.ResourceGroup);
            writer.WriteString("subscriptionId", this.SubscriptionId);
            //if (this.Resources.Count > 0)
            //{
            //    writer.WritePropertyName("resources");
            //    writer.WriteStartArray();
            //    foreach (var r in this.Resources)
            //    {
            //        JsonDocument.Parse(r.ToString()).RootElement.WriteTo(writer);
            //    }
            //    writer.WriteEndArray();
            //}

            foreach (var ex in this.ExtensionResource)
            {
                writer.WritePropertyName(ex.Key);
                JsonDocument.Parse(ex.Value).RootElement.WriteTo(writer);
            }
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        /// <summary>
        ///  the resources dependsOn me
        /// </summary>
        public List<string> Subsequent { get; set; } = new List<string>();
    }
}