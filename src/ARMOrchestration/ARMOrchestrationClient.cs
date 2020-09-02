using DurableTask.Core.Serializing;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration
{
    public class ARMOrchestrationClient
    {
        private readonly OrchestrationWorkerClient _OrchestrationWorkerClient;
        private readonly DataConverter _DataConverter = new JsonDataConverter();
        private readonly string _GetResourceListCommandString;
        private readonly string _GetAllResourceListCommandString;
        private readonly ARMOrchestrationOptions _Options;
        private readonly IServiceProvider _ServiceProvider;

        public ARMOrchestrationClient(
            OrchestrationWorkerClient orchestrationWorkerClient,
            IOptions<ARMOrchestrationOptions> options,
            IServiceProvider serviceProvider)
        {
            this._ServiceProvider = serviceProvider;
            this._OrchestrationWorkerClient = orchestrationWorkerClient;
            this._Options = options?.Value;

            this._GetResourceListCommandString = string.Format("select * from {0} where deploymentId=@deploymentId",
                this._Options.Database.DeploymentOperationsTableName);
            this._GetAllResourceListCommandString = string.Format("select * from {0} where RootId=@RootId",
                this._Options.Database.DeploymentOperationsTableName);
        }

        public async Task<DeploymentOperation> Run(DeploymentOrchestrationInput args)
        {
            if (string.IsNullOrEmpty(args.DeploymentId))
                throw new ArgumentNullException("DeploymentId");
            if (string.IsNullOrEmpty(args.ApiVersion))
                throw new ArgumentNullException("ApiVersion");
            if (string.IsNullOrEmpty(args.CorrelationId))
                throw new ArgumentNullException("CorrelationId");
            if (string.IsNullOrEmpty(args.DeploymentName))
                throw new ArgumentNullException("DeploymentName");
            if (string.IsNullOrEmpty(args.GroupId))
                throw new ArgumentNullException("GroupId");
            if (string.IsNullOrEmpty(args.GroupType))
                throw new ArgumentNullException("GroupType");
            if (string.IsNullOrEmpty(args.HierarchyId))
                throw new ArgumentNullException("HierarchyId");
            if (string.IsNullOrEmpty(args.TenantId))
                throw new ArgumentNullException("TenantId");
            if (string.IsNullOrEmpty(args.SubscriptionId) && string.IsNullOrEmpty(args.ManagementGroupId))
                throw new ArgumentException("SubscriptionId and ManagementGroupId must set one");
            if (!string.IsNullOrEmpty(args.SubscriptionId) && !string.IsNullOrEmpty(args.ManagementGroupId))
                throw new ArgumentException("SubscriptionId and ManagementGroupId only one can be set value");
            if (string.IsNullOrEmpty(args.CreateByUserId))
                throw new ArgumentNullException("CreateByUserId");
            if (args.ServiceProvider == null)
                args.ServiceProvider = _ServiceProvider;
            var instance = await _OrchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = args.DeploymentId,
                Orchestration = new OrchestrationSetting()
                {
                    Name = DeploymentOrchestration.Name,
                    Version = args.ApiVersion
                },
                Input = _DataConverter.Serialize(args)
            });
            return new DeploymentOperation(args)
            {
                RootId = args.DeploymentId,
                InstanceId = args.DeploymentId,
                ExecutionId = instance.ExecutionId,
                Stage = ProvisioningStage.Pending,
                Input = _DataConverter.Serialize(args)
            };
        }

        /// <summary>
        /// get the resources provisioned in this deployment, not include the nest deployment
        /// </summary>
        /// <param name="deploymentId"></param>
        /// <returns></returns>
        public async Task<List<DeploymentOperation>> GetResourceListAsync(string deploymentId)
        {
            List<DeploymentOperation> rs = new List<DeploymentOperation>();
            using (var db = new DbAccess(this._Options.Database.ConnectionString))
            {
                db.AddStatement(this._GetResourceListCommandString,
                    new Dictionary<string, object>() {
                        { "deploymentId",deploymentId}
                    });
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    rs.Add(new DeploymentOperation()
                    {
                        InstanceId = reader["InstanceId"].ToString(),
                        ExecutionId = reader["ExecutionId"].ToString(),
                        GroupId = reader["GroupId"].ToString(),
                        GroupType = reader["GroupType"].ToString(),
                        HierarchyId = reader["HierarchyId"].ToString(),
                        RootId = reader["RootId"].ToString(),
                        DeploymentId = reader["DeploymentId"].ToString(),
                        CorrelationId = reader["CorrelationId"].ToString(),
                        ResourceId = reader["ResourceId"].ToString(),
                        Name = reader["Name"].ToString(),
                        Type = reader["Type"].ToString(),
                        Stage = (ProvisioningStage)(int)reader["Stage"],
                        SubscriptionId = reader["SubscriptionId"]?.ToString(),
                        ManagementGroupId = reader["ManagementGroupId"].ToString(),
                        ParentResourceId = reader["ParentResourceId"]?.ToString(),
                        Input = reader["Input"].ToString(),
                        Result = reader["Result"]?.ToString(),
                        CreateByUserId = reader["CreateByUserId"].ToString(),
                        LastRunUserId = reader["LastRunUserId"].ToString()
                    });
                });
            }
            return rs;
        }

        /// <summary>
        /// get the resources provisioned in this deployment, include the nest deployment
        /// </summary>
        /// <param name="rootId"></param>
        /// <returns></returns>
        public async Task<List<DeploymentOperation>> GetAllResourceListAsync(string rootId)
        {
            List<DeploymentOperation> rs = new List<DeploymentOperation>();
            using (var db = new DbAccess(this._Options.Database.ConnectionString))
            {
                db.AddStatement(this._GetAllResourceListCommandString,
                    new Dictionary<string, object>() {
                        { "RootId",rootId}
                    });
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    rs.Add(new DeploymentOperation()
                    {
                        InstanceId = reader["InstanceId"].ToString(),
                        ExecutionId = reader["ExecutionId"].ToString(),
                        GroupId = reader["GroupId"].ToString(),
                        GroupType = reader["GroupType"].ToString(),
                        HierarchyId = reader["HierarchyId"].ToString(),
                        RootId = reader["RootId"].ToString(),
                        DeploymentId = reader["DeploymentId"].ToString(),
                        CorrelationId = reader["CorrelationId"].ToString(),
                        ResourceId = reader["ResourceId"].ToString(),
                        Name = reader["Name"].ToString(),
                        Type = reader["Type"].ToString(),
                        Stage = (ProvisioningStage)(int)reader["Stage"],
                        SubscriptionId = reader["SubscriptionId"]?.ToString(),
                        ManagementGroupId = reader["ManagementGroupId"].ToString(),
                        ParentResourceId = reader["ParentResourceId"]?.ToString(),
                        Input = reader["Input"].ToString(),
                        Result = reader["Result"]?.ToString(),
                        CreateByUserId = reader["CreateByUserId"].ToString(),
                        LastRunUserId = reader["LastRunUserId"].ToString()
                    });
                });
            }
            return rs;
        }
    }
}