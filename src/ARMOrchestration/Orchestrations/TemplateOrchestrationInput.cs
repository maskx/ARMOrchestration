using maskx.ARMOrchestration.ARMTemplate;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class TemplateOrchestrationInput
    {
        public string DeploymentId { get; set; }
        public string CorrelationId { get; set; }
        public string TemplateLink { get; set; }
        public string Template { get; set; }
        public string Parameters { get; set; }

        /// <summary>
        /// Complete  or Incremental
        /// </summary>
        public string Mode { get; set; } = "Incremental";

        /// <summary>
        /// Deployment Name
        /// </summary>
        public string Name { get; set; }

        public string ResourceGroup { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public string DeploymentId { get; set; }
        // public Template Template { get; set; }
    }
}