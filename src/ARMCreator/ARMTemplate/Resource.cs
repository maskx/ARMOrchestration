using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.OrchestrationCreator.ARMTemplate
{
    /// <summary>
    /// 部署服务需提供以下API
    /// https://docs.microsoft.com/en-us/rest/api/resources/
    /// </summary>
    public class Resource
    {
        public object Condition { get; set; }
        public string ApiVersion { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Tags { get; set; }
        public string Comments { get; set; }
        public Copy Copy { get; set; }

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
        public List<Resource> Resources { get; set; }
        public string ResourceGroup { get; set; }
        public string SubscriptionId { get; set; }

        public static Resource Parse(string str)
        {
            Resource resource = new Resource();
            using JsonDocument resDoc = JsonDocument.Parse(str);
            var root = resDoc.RootElement;
            if (root.TryGetProperty("condition", out JsonElement condition))
            {
                if (condition.ValueKind == JsonValueKind.True)
                    resource.Condition = true;
                else if (condition.ValueKind == JsonValueKind.False)
                    resource.Condition = false;
                else if (condition.ValueKind == JsonValueKind.String)
                    resource.Condition = condition.GetString();
            }
            if (root.TryGetProperty("apiVersion", out JsonElement apiVersion))
            {
                resource.ApiVersion = apiVersion.GetString();
            }
            else
            {
                throw new Exception("cannot find apiVersion property in template");
            }
            if (root.TryGetProperty("type", out JsonElement type))
            {
                resource.Type = type.GetString();
            }
            else
            {
                throw new Exception("cannot find type property in template");
            }
            if (root.TryGetProperty("name", out JsonElement name))
            {
                resource.Name = name.GetString();
            }
            else
            {
                throw new Exception("cannot find name property in template");
            }
            if (root.TryGetProperty("location", out JsonElement location))
            {
                resource.Location = location.GetString();
            }
            if (root.TryGetProperty("tags", out JsonElement tags))
            {
                resource.Tags = tags.GetString();
            }
            if (root.TryGetProperty("comments", out JsonElement comments))
            {
                resource.Comments = comments.GetString();
            }
            if (root.TryGetProperty("copy", out JsonElement copy))
            {
                resource.Copy = Copy.Parse(copy.GetString());
            }
            if (root.TryGetProperty("dependsOn", out JsonElement dependsOn))
            {
                foreach (var item in dependsOn.EnumerateArray())
                {
                    resource.DependsOn.Add(item.GetRawText());
                }
            }
            if (root.TryGetProperty("properties", out JsonElement properties))
            {
                resource.Properties = properties.GetString();
            }
            if (root.TryGetProperty("sku", out JsonElement sku))
            {
                resource.SKU = sku.GetString();
            }
            if (root.TryGetProperty("kind", out JsonElement kind))
            {
                resource.Kind = kind.GetString();
            }
            if (root.TryGetProperty("plan", out JsonElement plan))
            {
                resource.Plan = plan.GetString();
            }
            if (root.TryGetProperty("resources", out JsonElement resources))
            {
                foreach (var r in resources.EnumerateArray())
                {
                    resource.Resources.Add(Resource.Parse(r.GetRawText()));
                }
            }
            if (root.TryGetProperty("resourceGroup", out JsonElement resourceGroup))
            {
                resource.ResourceGroup = resourceGroup.GetString();
            }
            if (root.TryGetProperty("subscriptionId", out JsonElement subscriptionId))
            {
                resource.SubscriptionId = subscriptionId.GetString();
            }
            return resource;
        }
    }
}