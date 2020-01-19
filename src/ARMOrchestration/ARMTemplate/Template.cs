using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class Template
    {
        public const string ResourceGroupDeploymentLevel = "resourcegroup";
        public const string SubscriptionDeploymentLevel = "subscription";
        public const string TenantDeploymentLevel = "tenant";

        public string Schema { get; set; }

        public string ContentVersion { get; set; }

        public string ApiProfile { get; set; }

        public string Parameters { get; set; }

        public string Variables { get; set; }

        public Dictionary<string, Resource> Resources { get; set; } = new Dictionary<string, Resource>();
        public Dictionary<string, Copy> Copys = new Dictionary<string, Copy>();

        public Functions Functions { get; set; }

        public string Outputs { get; set; }

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
    }
}