using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace maskx.ARMOrchestration.Activities
{
    public class AsyncRequestActivityInput
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public DeploymentOrchestrationInput DeploymentContext { get; set; }
        public Resource Resource { get; set; }
        public ProvisioningStage ProvisioningStage { get; set; }
        public Dictionary<string, object> Context { get; set; }
        private IServiceProvider _ServiceProvider;

        [JsonIgnore]
        internal IServiceProvider ServiceProvider
        {
            get { return _ServiceProvider; }
            set
            {
                // this should be set first after deserialize
                _ServiceProvider = value;
                DeploymentContext.ServiceProvider = value;
                Resource.Input = DeploymentContext;
            }
        }
    }
}