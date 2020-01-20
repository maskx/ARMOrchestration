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

        public static (bool Result, string Message, Resource resource)
            Parse(string jsonString,
            Dictionary<string, object> context,
            ARMOrchestrationOptions options,
            string parentName = "",
            string parentType = "")
        {
            DeploymentContext deploymentContext = context["armcontext"] as DeploymentContext;
            Resource r = new Resource();
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;
            if (root.TryGetProperty("condition", out JsonElement condition))
            {
                if (condition.ValueKind == JsonValueKind.False)
                    r.Condition = false;
                else if (condition.ValueKind == JsonValueKind.String)
                    r.Condition = (bool)ARMFunctions.Evaluate(condition.GetString(), context);
            }
            if (root.TryGetProperty("apiVersion", out JsonElement apiVersion))
                r.ApiVersion = apiVersion.GetString();
            else
                return (false, "not find apiVersion in resource node", null);
            if (root.TryGetProperty("type", out JsonElement type))
            {
                r.Type = ARMFunctions.Evaluate(type.GetString(), context).ToString();
                if (!string.IsNullOrEmpty(parentType))
                    r.Type = $"{parentType}/{r.Type}";
            }
            else
                return (false, "not find type in resource node", null);
            if (root.TryGetProperty("name", out JsonElement name))
            {
                r.Name = ARMFunctions.Evaluate(name.GetString(), context).ToString();
                if (!string.IsNullOrEmpty(parentName))
                    r.Name = $"{parentName}/{r.Name}";
            }
            else
                return (false, "not find name in resource node", null);
            if (root.TryGetProperty("location", out JsonElement location))
                r.Location = ARMFunctions.Evaluate(location.GetString(), context).ToString();
            if (root.TryGetProperty("tags", out JsonElement tags))
                r.Tags = tags.GetRawText();
            if (root.TryGetProperty("comments", out JsonElement comments))
                r.Comments = comments.GetString();
            if (root.TryGetProperty("dependsOn", out JsonElement dependsOn))
            {
                r.DependsOn = new List<string>();
                using var dd = JsonDocument.Parse(dependsOn.GetRawText());
                foreach (var item in dd.RootElement.EnumerateArray())
                {
                    r.DependsOn.Add(ARMFunctions.Evaluate(item.GetString(), context).ToString());
                }
            }
            if (root.TryGetProperty("properties", out JsonElement properties))
                r.Properties = properties.ExpandObject(context);
            if (root.TryGetProperty("sku", out JsonElement sku))
                r.SKU = sku.GetString();
            if (root.TryGetProperty("kind", out JsonElement kind))
                r.Kind = kind.GetString();
            if (root.TryGetProperty("plan", out JsonElement plan))
                r.Plan = plan.GetString();
            if (root.TryGetProperty("resourceGroup", out JsonElement resourceGroup))
                r.ResourceGroup = ARMFunctions.Evaluate(resourceGroup.GetString(), context).ToString();
            else
                r.ResourceGroup = deploymentContext.ResourceGroup;
            if (root.TryGetProperty("subscriptionId", out JsonElement subscriptionId))
                r.SubscriptionId = ARMFunctions.Evaluate(subscriptionId.GetString(), context).ToString();
            else
                r.SubscriptionId = deploymentContext.SubscriptionId;

            #region ResouceId

            var t = deploymentContext.Template;
            if (t.DeployLevel == Template.ResourceGroupDeploymentLevel)
                r.ResouceId = ARMFunctions.resourceId(
                    deploymentContext,
                    r.SubscriptionId,
                    r.ResourceGroup,
                    r.Type,
                    r.Name);
            else if (t.DeployLevel == Template.SubscriptionDeploymentLevel)
                r.ResouceId = ARMFunctions.subscriptionResourceId(deploymentContext, r.SubscriptionId, r.Type, r.Name);
            else
                r.ResouceId = ARMFunctions.tenantResourceId(r.Type, r.Name);

            #endregion ResouceId

            if (root.TryGetProperty("resources", out JsonElement resources))
            {
                foreach (var childres in resources.EnumerateArray())
                {
                    //https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/child-resource-name-type
                    var childResult = Resource.Parse(childres.GetRawText(), context, options, r.Name, r.Type);
                    if (childResult.Result)
                    {
                        r.Resources.Add(childResult.resource);
                    }
                    else
                        return (false, childResult.Message, null);
                }
            }

            foreach (var item in options.ExtensionResources)
            {
                if (root.TryGetProperty(item, out JsonElement e))
                {
                    r.ExtensionResource.Add(item, e.GetRawText());
                }
            }
            return (true, string.Empty, r);
        }

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