﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace maskx.OrchestrationCreator.ARMTemplate
{
    public class Template : IDisposable
    {
        public const string ResourceGroupDeploymentLevel = "resourcegroup";
        public const string SubscriptionDeploymentLevel = "subscription";
        public const string TenantDeploymentLevel = "tenant";

        public string Schema
        {
            get
            {
                if (root.TryGetProperty("$schema", out JsonElement schema))
                {
                    return schema.GetString();
                }
                return string.Empty;
            }
        }

        public string ContentVersion
        {
            get
            {
                if (root.TryGetProperty("contentVersion", out JsonElement contentVersion))
                {
                    return contentVersion.GetString();
                }
                return string.Empty;
            }
        }

        public string ApiProfile
        {
            get
            {
                if (root.TryGetProperty("apiProfile", out JsonElement apiProfile))
                {
                    return apiProfile.GetString();
                }
                return string.Empty;
            }
        }

        public string Parameters
        {
            get
            {
                if (root.TryGetProperty("parameters", out JsonElement parameters))
                {
                    return parameters.GetRawText();
                }
                return string.Empty;
            }
        }

        public string Variables
        {
            get
            {
                if (root.TryGetProperty("variables", out JsonElement variables))
                {
                    return variables.GetRawText();
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

        public Functions Functions
        {
            get
            {
                if (root.TryGetProperty("functions", out JsonElement functions))
                {
                    return new Functions(functions.GetRawText());
                }
                return null;
            }
        }

        public string Outputs
        {
            get
            {
                if (root.TryGetProperty("outputs", out JsonElement outputs))
                {
                    return outputs.GetRawText();
                }
                return string.Empty;
            }
        }

        private string _DeployLevel = string.Empty;

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-to-subscription
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-to-management-group
        /// </summary>
        public string DeployLevel
        {
            get
            {
                if (string.IsNullOrEmpty(_DeployLevel))
                {
                    if (this.Schema.EndsWith("deploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                        _DeployLevel = ResourceGroupDeploymentLevel;
                    else if (this.Schema.EndsWith("subscriptionDeploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                        _DeployLevel = SubscriptionDeploymentLevel;
                    else if (this.Schema.EndsWith("managementGroupDeploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                        _DeployLevel = TenantDeploymentLevel;
                }
                return _DeployLevel;
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

        private Dictionary<string, object> context;

        public Template(string jsonString, Dictionary<string, object> context)
        {
            this.jsonString = jsonString;
            this.context = context;
        }

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
    }
}