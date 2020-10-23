using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using Newtonsoft.Json;
using System;

namespace maskx.ARMOrchestration.Activities
{
    public class AsyncRequestActivityInput
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public Resource Resource { get; set; }
        public Deployment Input { get; set; }
        public ProvisioningStage ProvisioningStage { get; set; }
        private IServiceProvider _ServiceProvider;

        [JsonIgnore]
        public IServiceProvider ServiceProvider
        {
            get { return _ServiceProvider; }
            set
            {
                // this should be set first after deserialize
                _ServiceProvider = value;
                Input.ServiceProvider = value;
                Resource.Input = Input;
            }
        }
    }
}