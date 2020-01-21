using maskx.ARMOrchestration.ARMTemplate;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestrationInput
    {
        /// <summary>
        ///
        /// </summary>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/rollback-on-error"/>
        public bool RollbackToLastDeployment { get; set; } = false;

        public string InstanceId { get; set; }
        public string CorrelationId { get; set; }
        public TemplateLink TemplateLink { get; set; }
        public string Template { get; set; }
        public ParametersLink ParametersLink { get; set; }
        public string Parameters { get; set; }

        /// <summary>
        /// Complete  or Incremental
        /// </summary>
        public DeploymentMode Mode { get; set; } = DeploymentMode.Incremental;

        /// <summary>
        /// Deployment Name
        /// </summary>
        public string Name { get; set; }

        public string ResourceGroup { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
    }
}