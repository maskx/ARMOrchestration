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
        [JsonProperty]
        string _DeploymentOperationId;
        [JsonIgnore]
        public string DeploymentOperationId
        {
            get
            {
                if (IsRetry && string.IsNullOrEmpty(_DeploymentOperationId))
                {
                    LoadFromDb();
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
                    LoadFromDb();
                }
                return _Resource;
            }
            set
            {
                _Resource = value;
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

        private void LoadFromDb()
        {
            if (CopyIndex == -1)
            {
                _Resource = Deployment.GetFirstResource(ResourceId);
            }
            else
            {
                foreach (var r in Deployment.Template.Resources)
                {
                    if (r.ResourceId == ResourceId)
                    {
                        _Resource = r.Copy.GetResource(CopyIndex);
                        break;
                    }
                }
            }
            bool loadFromDb = false;
            var options = ServiceProvider.GetService<IOptions<ARMOrchestrationOptions>>().Value;
            using var db = new SQLServerAccess(options.Database.ConnectionString, ServiceProvider.GetService<ILoggerFactory>());
            db.AddStatement($"select Id,input from {options.Database.DeploymentOperationsTableName} where DeploymentId=@DeploymentId and ResourceId=@ResourceId",
                new
                {
                    DeploymentId,
                    _Resource.ResourceId
                });
            db.ExecuteReaderAsync((reader, index) =>
            {

                _DeploymentOperationId = reader.GetString(0);
                if (!reader.IsDBNull(1))
                {
                    loadFromDb = true;
                    _Resource.Change(reader.GetString(1));
                }
            }).Wait();
            // 仅Resouce报文做运行时展开后才会保存到数据库Input字段
            if (!loadFromDb)
            {
                IPolicyService policyService = ServiceProvider.GetService<IPolicyService>();
                if (policyService != null)
                {
                    policyService.Evaluateesource(_Resource);
                }
            }
        }
    }
}
