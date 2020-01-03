using maskx.ARMOrchestration.ARMTemplate;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestrationInput
    {
        public string DeploymentId { get; set; }
        public (string Resource, string Type, string ResourceId) ParentResource { get; set; }
        public string Resource { get; set; }
        public Dictionary<string, object> OrchestrationContext { get; set; }
    }
}