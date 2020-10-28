using maskx.ARMOrchestration.ARMTemplate;
using Newtonsoft.Json;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResInput
    {
        public string DeploymentResourceId { get; set; }
        public string NameWithServiceType { get; set; }
        public int CopyIndex { get; set; }
        private Deployment _Deployment;
        [JsonIgnore]
        public Deployment Deployment
        {
            get
            {
                if (_Deployment == null)
                {
                    _Deployment = _ServiceProvider.GetService<ARMTemplateHelper>().GetDeploymentByResourceIdAsync(this.DeploymentResourceId).Result;
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
                    if (_Resource.Copy != null)
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
