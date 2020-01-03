using maskx.ARMOrchestration.ARMTemplate;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestrationInput
    {
        public string Resource { get; set; }
        public Dictionary<string, object> OrchestrationContext { get; set; }
    }
}