using maskx.ARMOrchestration.ARMTemplate;

namespace maskx.ARMOrchestration.WhatIf
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#uri-parameters"/>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#request-body"/>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#deploymentwhatifproperties"/>
    public class PredictTemplateOrchestrationInput
    {
        public string Location { get; set; }
        public string DeploymentName { get; set; }
        public DeploymentMode Mode { get; set; }
        public string SubscriptionId { get; set; }
        public string ResourceGroupName { get; set; }
        public TemplateLink TemplateLink { get; set; }
        public ParametersLink ParametersLink { get; set; }
        public string Template { get; set; }
        public string Parameters { get; set; }
        public WhatIfResultFormat ResultFormat { get; set; }
        public ScopeType ScopeType { get; set; }
        public string TenantId { get; set; }
        public string CorrelationId { get; set; }
    }
}