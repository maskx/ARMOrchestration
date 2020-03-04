using maskx.ARMOrchestration.ARMTemplate;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestrationInput : DeploymentContext
    {
        public TemplateLink TemplateLink { get; set; }
        public ParametersLink ParametersLink { get; set; }
        public string TemplateContent { get; set; }
        public List<DeploymentOrchestrationInput> Deployments { get; set; } = new List<DeploymentOrchestrationInput>();
    }
}