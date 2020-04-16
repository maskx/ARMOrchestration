using ARMOrchestrationTest.Mock;
using System;
using System.Collections.Generic;

namespace ARMOrchestrationTest.PluginTests
{
    public class PluginInfrastructure : MockInfrastructure
    {
        public PluginInfrastructure(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            this.BeforeDeploymentOrchestration = new List<(string Name, string Version)>() { (typeof(BeforeDeploymentOrchestration).FullName, "") };
            this.AfterDeploymentOrhcestration = new List<(string Name, string Version)>() { (typeof(AfterDeploymentOrhcestration).FullName, "") };
            this.BeforeResourceProvisioningOrchestation = new List<(string Name, string Version)>() { (typeof(BeforeResourceProvisioningOrchestation).FullName, "") };
            this.AfterResourceProvisioningOrchestation = new List<(string Name, string Version)>() { (typeof(AfterResourceProvisioningOrchestation).FullName, "") };
        }
    }
}