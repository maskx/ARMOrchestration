using maskx.OrchestrationCreator.ARMTemplate;
using System.Collections.Generic;

namespace maskx.OrchestrationCreator
{
    public class ResourceOrchestrationInput
    {
        public Resource Resource { get; set; }
        public Dictionary<string, object> OrchestrationContext { get; set; }
    }
}