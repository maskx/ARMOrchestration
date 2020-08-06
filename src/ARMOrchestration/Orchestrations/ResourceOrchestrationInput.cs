using maskx.ARMOrchestration.ARMTemplate;
using Newtonsoft.Json;
using System;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestrationInput
    {
        public bool IsRedeployment { get; set; } = false;
        public Resource Resource { get; set; }
        public DeploymentOrchestrationInput Context { get; set; }
        private IServiceProvider _ServiceProvider;

        [JsonIgnore]
        public IServiceProvider ServiceProvider
        {
            get { return _ServiceProvider; }
            set
            {
                // this should be set first after deserialize
                _ServiceProvider = value;
                Context.ServiceProvider = value;
                Resource.Input = Context;
            }
        }
    }
}