using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestrationInput
    {
        public bool IsRetry { get; set; } = false;
        string _DeploymentOperationId;
        [JsonIgnore]
        public string DeploymentOperationId
        {
            get
            {
                if (string.IsNullOrEmpty(_DeploymentOperationId))
                {
                    if (IsRetry)
                    {
                        var options = ServiceProvider.GetService<IOptions<ARMOrchestrationOptions>>().Value;
                        using var db = new SQLServerAccess(options.Database.ConnectionString, ServiceProvider.GetService<ILoggerFactory>());
                        db.AddStatement($"select Id from {options.Database.DeploymentOperationsTableName} where DeploymentId=N'{DeploymentId}' and ResourceId=N'{ResourceId}'");
                        _DeploymentOperationId = db.ExecuteScalarAsync().Result?.ToString();
                    }
                }
                return _DeploymentOperationId;
            }
            set { _DeploymentOperationId = value; }
        }
        public string LastRunUserId { get; set; }
        public string DeploymentId { get; set; }
        public string ResourceId { get; set; }
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
                    _Resource = Deployment.GetFirstResource(ResourceId);
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
