using maskx.ARMOrchestration.ARMTemplate;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestrationInput : DeploymentContext
    {
 
        public List<string> DependsOn { get; set; } = new List<string>();
        public Dictionary<string, DeploymentOrchestrationInput> Deployments { get; set; } = new Dictionary<string, DeploymentOrchestrationInput>();
    }
}