using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService.Activity;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration
{
    public class ARMOrchestrationOptions
    {
        public IListFunction ListFunction { get; set; }
        public Dictionary<string, Func<ResourceOrchestrationInput, string, string, AsyncRequestInput>> ExtensionResources { get; set; }
    }
}