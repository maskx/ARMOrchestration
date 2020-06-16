using System.Collections.Generic;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class Template
    {
        public string Schema { get; set; }

        public string ContentVersion { get; set; }

        public string ApiProfile { get; set; }

        public string Parameters { get; set; }

        public string Variables { get; set; }

        public ResourceCollection Resources { get; set; } = new ResourceCollection();

        public Functions Functions { get; set; }

        public string Outputs { get; set; }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-to-subscription
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-to-management-group
        /// </summary>
        public DeployLevel DeployLevel { get; set; }

        internal List<string> ConditionFalseResources { get; private set; } = new List<string>();
    }
}