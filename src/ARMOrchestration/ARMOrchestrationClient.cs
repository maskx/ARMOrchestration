using DurableTask.Core;
using DurableTask.Core.Serializing;
using Dynamitey.DynamicObjects;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration
{
    public class ARMOrchestrationClient
    {
        private readonly OrchestrationWorkerClient orchestrationWorkerClient;
        private readonly string TemplateOrchestrationUri = typeof(DeploymentOrchestration).FullName + "_";
        private readonly DataConverter DataConverter = new JsonDataConverter();
        private string getResourceListCommandString = "select * from {0} where deploymentId=@deploymentId";
        private readonly ARMOrchestrationOptions options;

        public ARMOrchestrationClient(OrchestrationWorkerClient orchestrationWorkerClient, IOptions<ARMOrchestrationOptions> options)
        {
            this.orchestrationWorkerClient = orchestrationWorkerClient;
            this.options = options?.Value;
        }

        public async Task<OrchestrationInstance> Run(DeploymentOrchestrationInput args)
        {
            return await orchestrationWorkerClient.JumpStartOrchestrationAsync(new Job
            {
                InstanceId = args.DeploymentId,
                Orchestration = new OrchestrationSetting()
                {
                    Creator = "DICreator",
                    Uri = TemplateOrchestrationUri
                },
                Input = DataConverter.Serialize(args)
            });
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
    }
}