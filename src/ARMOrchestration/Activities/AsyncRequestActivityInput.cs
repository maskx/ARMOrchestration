using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Activities
{
    public class AsyncRequestActivityInput
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public DeploymentContext DeploymentContext { get; set; }
        public Resource Resource { get; set; }
        public ProvisioningStage ProvisioningStage { get; set; }
        public Dictionary<string, object> Context { get; set; }
    }
}