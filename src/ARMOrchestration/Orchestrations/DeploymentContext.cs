﻿using maskx.ARMOrchestration.ARMTemplate;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentContext
    {
        public string DeploymentId { get; set; }
        public string CorrelationId { get; set; }
        public string ResourceGroup { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public DeploymentMode Mode { get; set; } = DeploymentMode.Incremental;
        public Template Template { get; set; }
        public string Parameters { get; set; }
    }
}