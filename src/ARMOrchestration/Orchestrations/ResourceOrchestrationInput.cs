using maskx.ARMOrchestration.ARMTemplate;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestrationInput
    {
        public bool IsRetry { get; set; } = false;
        public string DeploymentOperationId { get; set; }
        public string LastRunUserId { get; set; }
        public string DeploymentId { get; set; }
        public string NameWithServiceType { get; set; }
        public int CopyIndex { get; set; } = -1;
        private Deployment _Deployment;
        [JsonIgnore]
        public Deployment Deployment
        {
            get
            {
                if (_Deployment == null)
                {
                    _Deployment = _ServiceProvider.GetService<ARMTemplateHelper>().GetDeploymentAsync(this.DeploymentId).Result;
                }
                return _Deployment;
            }
        }
        private Resource _Resource;
        [JsonIgnore]
        public Resource Resource
        {
            get
            {
                if (_Resource == null)
                {
                    _Resource = Deployment.GetFirstResource(NameWithServiceType);
                    if (_Resource.Copy != null && CopyIndex != -1)
                    {
                        _Resource = _Resource.Copy.GetResource(CopyIndex);
                    }
                }
                return _Resource;
            }
        }
        private IServiceProvider _ServiceProvider;

        /// <summary>
        /// // this should be set first after deserialize
        /// </summary>
        [JsonIgnore]
        public IServiceProvider ServiceProvider
        {
            get { return _ServiceProvider; }
            set { _ServiceProvider = value; }
        }

    }
}
