using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestrationInput
    {
        public bool IsRedeployment { get; set; } = false;
        public Resource Resource { get; set; }
        public DeploymentOrchestrationInput Input { get; set; }
        private IServiceProvider _ServiceProvider;

        /// <summary>
        /// // this should be set first after deserialize
        /// </summary>
        [JsonIgnore]
        public IServiceProvider ServiceProvider
        {
            get { return _ServiceProvider; }
            set
            {
                _ServiceProvider = value;
                Input.ServiceProvider = value;
                Resource.Input = Input;
            }
        }
    }
}