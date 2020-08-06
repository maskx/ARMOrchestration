using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Activities
{
    public class WaitDependsOnActivityInput
    {
        public DeploymentOrchestrationInput DeploymentContext { get; set; }
        public Resource Resource { get; set; }
        public ProvisioningStage ProvisioningStage { get; set; }
        public List<string> DependsOn { get; set; }
        private IServiceProvider _ServiceProvider;

        internal IServiceProvider ServiceProvider
        {
            get { return _ServiceProvider; }
            set
            {
                // this should be set first after deserialize
                _ServiceProvider = value;
                DeploymentContext.ServiceProvider = value;
                if (Resource != null)
                    Resource.Input = DeploymentContext;
            }
        }
    }
}