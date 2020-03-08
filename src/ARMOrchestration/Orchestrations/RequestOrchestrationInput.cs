﻿using maskx.ARMOrchestration.ARMTemplate;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class RequestOrchestrationInput
    {
        public DeploymentContext DeploymentContext { get; set; }
        public Resource Resource { get; set; }
        public RequestAction RequestAction { get; set; }
        public Dictionary<string, object> Context { get; set; }
    }
}