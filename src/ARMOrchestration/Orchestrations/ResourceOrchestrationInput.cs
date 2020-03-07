using maskx.ARMOrchestration.ARMTemplate;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestrationInput
    {
        public bool IsRedeployment { get; set; } = false;
        public Resource Resource { get; set; }
        public DeploymentContext Context { get; set; }
    }
}