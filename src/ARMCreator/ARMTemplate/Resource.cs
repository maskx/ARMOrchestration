﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace maskx.OrchestrationCreator.ARMTemplate
{
    /// <summary>
    /// 部署服务需提供以下API
    /// https://docs.microsoft.com/en-us/rest/api/resources/
    /// </summary>
    public class Resource : IDisposable
    {
        public bool Condition
        {
            get
            {
                if (!root.TryGetProperty("condition", out JsonElement condition))
                    return true;
                if (condition.ValueKind == JsonValueKind.True)
                    return true;
                if (condition.ValueKind == JsonValueKind.False)
                    return false;
                if (condition.ValueKind == JsonValueKind.String)
                    return (bool)ARMFunctions.Evaluate(condition.GetString(), context);
                return true;
            }
        }

        public string ApiVersion
        {
            get
            {
                if (root.TryGetProperty("apiVersion", out JsonElement apiVersion))
                {
                    return apiVersion.GetString();
                }
                return string.Empty;
            }
        }

        public string Type
        {
            get
            {
                if (root.TryGetProperty("type", out JsonElement type))
                {
                    return type.GetString();
                }
                return string.Empty;
            }
        }

        public string Name
        {
            get
            {
                if (root.TryGetProperty("name", out JsonElement name))
                {
                    return ARMFunctions.Evaluate(name.GetString(), this.context).ToString();
                }
                return string.Empty;
            }
        }

        public string Location
        {
            get
            {
                if (root.TryGetProperty("location", out JsonElement location))
                {
                    return ARMFunctions.Evaluate(location.GetString(), this.context).ToString();
                }
                return string.Empty;
            }
        }

        public string Tags
        {
            get
            {
                if (root.TryGetProperty("tags", out JsonElement tags))
                {
                    return tags.GetString();
                }
                return string.Empty;
            }
        }

        public string Comments
        {
            get
            {
                if (root.TryGetProperty("comments", out JsonElement comments))
                {
                    return comments.GetString();
                }
                return string.Empty;
            }
        }

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
        public IEnumerable<string> DependsOn
        {
            get
            {
                if (root.TryGetProperty("dependsOn", out JsonElement dependsOn))
                {
                    return dependsOn.EnumerateArray().Select((e) => e.GetRawText());
                }
                return null;
            }
        }

        public string Properties
        {
            get
            {
                if (root.TryGetProperty("properties", out JsonElement properties))
                {
                    return properties.GetRawText();
                }
                return string.Empty;
            }
        }

        public string SKU
        {
            get
            {
                if (root.TryGetProperty("sku", out JsonElement sku))
                {
                    return sku.GetRawText();
                }
                return string.Empty;
            }
        }

        public string Kind
        {
            get
            {
                if (root.TryGetProperty("kind", out JsonElement kind))
                {
                    return kind.GetString();
                }
                return string.Empty;
            }
        }

        public string Plan
        {
            get
            {
                if (root.TryGetProperty("plan", out JsonElement plan))
                {
                    return plan.GetString();
                }
                return string.Empty;
            }
        }

        public IEnumerable<Resource> Resources
        {
            get
            {
                if (root.TryGetProperty("resources", out JsonElement resources))
                {
                    return resources.EnumerateArray().Select((e) => new Resource(e.GetRawText(), this.context));
                }
                return null;
            }
        }

        public string ResourceGroup
        {
            get
            {
                if (root.TryGetProperty("resourceGroup", out JsonElement resourceGroup))
                {
                    return ARMFunctions.Evaluate(resourceGroup.GetString(), this.context).ToString();
                }
                return armInput.ResourceGroup;
            }
        }

        public string SubscriptionId
        {
            get
            {
                if (root.TryGetProperty("subscriptionId", out JsonElement subscriptionId))
                {
                    return ARMFunctions.Evaluate(subscriptionId.GetString(), this.context).ToString();
                }
                return this.armInput.SubscriptionId; ;
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

        private ARMOrchestrationInput armInput;

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

        public Resource(string jsonString, Dictionary<string, object> context)
        {
            this.jsonString = jsonString;
            this.context = context;
            this.armInput = context["armcontext"] as ARMOrchestrationInput;
        }
    }
}