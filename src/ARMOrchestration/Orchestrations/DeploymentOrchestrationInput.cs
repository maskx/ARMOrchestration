namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestrationInput
    {
        public string DeploymentId { get; set; }
        public string CorrelationId { get; set; }
        public string TemplateLink { get; set; }
        public string Template { get; set; }
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