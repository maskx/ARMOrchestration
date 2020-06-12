﻿using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Activities
{
    public class WaitDependsOnActivityInput
    {
        public DeploymentContext DeploymentContext { get; set; }
        public Resource Resource { get; set; }
        public ProvisioningStage ProvisioningStage { get; set; }
        public List<string> DependsOn { get; set; }
    }
}