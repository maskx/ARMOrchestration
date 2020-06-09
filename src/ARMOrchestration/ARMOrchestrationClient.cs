using DurableTask.Core;
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
        private readonly OrchestrationWorkerClient orchestrationWorkerClient;
        private readonly DataConverter DataConverter = new JsonDataConverter();
        private string getResourceListCommandString = "select * from {0} where deploymentId=@deploymentId";
        private string getAllResourceListCommandString = "select * from {0} where RootId=@RootId";
        private readonly ARMOrchestrationOptions options;
        private readonly ARMTemplateHelper _ARMTemplateHelper;
        private readonly IInfrastructure _Infrastructure;

        public ARMOrchestrationClient(
            OrchestrationWorkerClient orchestrationWorkerClient,
            IOptions<ARMOrchestrationOptions> options,
            ARMTemplateHelper aRMTemplateHelper,
            IInfrastructure infrastructure)
        {
            this.orchestrationWorkerClient = orchestrationWorkerClient;
            this.options = options?.Value;
            this._ARMTemplateHelper = aRMTemplateHelper;
            this._Infrastructure = infrastructure;
        }

        public async Task<OrchestrationInstance> Run(DeploymentOrchestrationInput args)
        {
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
            var instance = await orchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = args.DeploymentId,
                Orchestration = new OrchestrationSetting()
                {
                    Name = DeploymentOrchestration.Name,
                    Version = args.ApiVersion
                },
                Input = DataConverter.Serialize(args)
            });
            this._ARMTemplateHelper.SaveDeploymentOperation(new DeploymentOperation(args, this._Infrastructure)
            {
                RootId = args.DeploymentId,
                ParentResourceId = $"{instance.InstanceId}:{instance.ExecutionId}",
                InstanceId = instance.InstanceId,
                ExecutionId = instance.ExecutionId,
                Stage = ProvisioningStage.Pending,
                Input = DataConverter.Serialize(args)
            });
            return instance;
        }

        public async Task<List<DeploymentOperation>> GetResourceListAsync(string deploymentId)
        {
            List<DeploymentOperation> rs = new List<DeploymentOperation>();
            using (var db = new DbAccess(this.options.Database.ConnectionString))
            {
                db.AddStatement(string.Format(this.getResourceListCommandString, this.options.Database.DeploymentOperationsTableName),
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
                        Result = reader["Result"]?.ToString()
                    });
                });
            }
            return rs;
        }

        public async Task<List<DeploymentOperation>> GetAllResourceListAsync(string rootId)
        {
            List<DeploymentOperation> rs = new List<DeploymentOperation>();
            using (var db = new DbAccess(this.options.Database.ConnectionString))
            {
                db.AddStatement(string.Format(this.getAllResourceListCommandString, this.options.Database.DeploymentOperationsTableName),
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
                        Result = reader["Result"]?.ToString()
                    });
                });
            }
            return rs;
        }
    }
}