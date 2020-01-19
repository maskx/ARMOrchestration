using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.Extensions;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// 部署服务需提供以下API
    /// https://docs.microsoft.com/en-us/rest/api/resources/
    /// </summary>
    public class Resource : IDisposable
    {
        public bool Condition { get; set; } = true;

        public string ApiVersion { get; set; }

        public string Type { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public string Tags { get; set; }

        public string Comments { get; set; }

        public Copy Copy
        {
            get
            {
                if (root.TryGetProperty("copy", out JsonElement copy))
                {
                    return new Copy(copy.GetRawText(), this.context);
                }
                return null;
            }
        }

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
        public string DependsOn { get; set; }

        public string Properties { get; set; }

        public string SKU { get; set; }

        public string Kind { get; set; }

        public string Plan { get; set; }

        public List<Resource> Resources { get; set; } = new List<Resource>();

        public string ResourceGroup { get; set; }

        public string SubscriptionId { get; set; }

        public string ResouceId
        {
            get
            {
                var p = context["armcontext"] as TemplateOrchestrationInput;
                var t = new Template(p.Template, context);
                if (t.DeployLevel == Template.ResourceGroupDeploymentLevel)
                    return ARMFunctions.resourceId(
                        p,
                        this.SubscriptionId,
                        this.ResourceGroup,
                        this.Type,
                        this.Name);
                else if (t.DeployLevel == Template.SubscriptionDeploymentLevel)
                    return ARMFunctions.subscriptionResourceId(p, this, SubscriptionId, this.Type, this.Name);
                else
                    return ARMFunctions.tenantResourceId(this.Type, this.Name);
            }
        }

        private string jsonString;
        private JsonDocument jsonDoc;

        private JsonElement root
        {
            get
            {
                if (jsonDoc == null)
                    jsonDoc = JsonDocument.Parse(jsonString);
                return jsonDoc.RootElement;
            }
        }

        private TemplateOrchestrationInput armInput;

        public override string ToString()
        {
            return this.jsonString;
        }

        public void Dispose()
        {
            if (this.jsonDoc != null)
            {
                jsonDoc.Dispose();
            }
        }

        private Dictionary<string, object> context;

        public Resource()
        {
        }

        public static (bool Result, string Message, Resource resource) Parse(string jsonString, Dictionary<string, object> context)
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
            if (!r.Condition)
                return (true, string.Empty, r);
            if (root.TryGetProperty("apiVersion", out JsonElement apiVersion))
                r.ApiVersion = apiVersion.GetString();
            else
                return (false, "not find apiVersion in resource node", null);
            if (root.TryGetProperty("type", out JsonElement type))
                r.Type = ARMFunctions.Evaluate(type.GetString(), context).ToString();
            else
                return (false, "not find type in resource node", null);
            if (root.TryGetProperty("name", out JsonElement name))
                r.Name = ARMFunctions.Evaluate(name.GetString(), context).ToString();
            else
                return (false, "not find name in resource node", null);
            if (root.TryGetProperty("location", out JsonElement location))
                r.Location = ARMFunctions.Evaluate(location.GetString(), context).ToString();
            if (root.TryGetProperty("tags", out JsonElement tags))
                r.Tags = tags.GetRawText();
            if (root.TryGetProperty("comments", out JsonElement comments))
                r.Comments = comments.GetString();
            if (root.TryGetProperty("dependsOn", out JsonElement dependsOn))
                r.DependsOn = dependsOn.GetRawText();
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
            if (root.TryGetProperty("resources", out JsonElement resources))
            {
                foreach (var childres in resources.EnumerateArray())
                {
                    var childResult = Resource.Parse(childres.GetRawText(), context);
                    if (childResult.Result)
                        r.Resources.Add(childResult.resource);
                    else
                        return (false, childResult.Message, null);
                }
            }
            return (true, string.Empty, r);
        }

        public Resource(string jsonString, Dictionary<string, object> context)
        {
            this.jsonString = jsonString;
            this.context = context;
            this.armInput = context["armcontext"] as TemplateOrchestrationInput;
        }

        public bool TryGetExtensionResource(string name, out string resource)
        {
            resource = null;
            if (root.TryGetProperty(name, out JsonElement r))
            {
                resource = r.GetRawText();
                return true;
            }

            return false;
        }
    }
}