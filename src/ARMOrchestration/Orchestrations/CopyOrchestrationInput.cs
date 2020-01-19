using maskx.ARMOrchestration.ARMTemplate;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class CopyOrchestrationInput
    {
        public Copy Copy { get; set; }
        public DeploymentContext Context { get; set; }
    }
}