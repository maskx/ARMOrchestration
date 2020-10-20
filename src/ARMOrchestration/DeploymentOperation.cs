using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.DependencyInjection;

namespace maskx.ARMOrchestration
{
    public class DeploymentOperation
    {
        public DeploymentOperation()
        {
        }

        public DeploymentOperation(DeploymentOrchestrationInput deploymentInput, Resource resource = null)
        {
            this.GroupType = deploymentInput.GroupType;
            this.GroupId = deploymentInput.GroupId;
            this.HierarchyId = deploymentInput.HierarchyId;
            this.RootId = deploymentInput.RootId;
            this.DeploymentId = deploymentInput.DeploymentId;
            this.CorrelationId = deploymentInput.CorrelationId;
            this.CreateByUserId = deploymentInput.CreateByUserId;
            if (string.IsNullOrEmpty(deploymentInput.LastRunUserId))
                this.LastRunUserId = this.CreateByUserId;
            else
                this.LastRunUserId = deploymentInput.LastRunUserId;
            if (resource == null)
            {
                IInfrastructure infrastructure = deploymentInput.ServiceProvider.GetService<IInfrastructure>();
                this.ResourceId = deploymentInput.ResourceId;
                this.Type = infrastructure.BuiltinServiceTypes.Deployments;
                this.Name = deploymentInput.Name;
                this.ParentResourceId = deploymentInput.ParentId;
                this.SubscriptionId = deploymentInput.SubscriptionId;
                this.ManagementGroupId = deploymentInput.ManagementGroupId;
            }
            else
            {
                this.ResourceId = resource.ResourceId;
                this.Name = resource.Name;
                this.Type = (resource.Copy != null && !resource.CopyIndex.HasValue) ? resource.Copy.Type : resource.Type;
                this.ParentResourceId = resource.CopyIndex.HasValue ? resource.Copy.Id : deploymentInput.ResourceId;
                this.SubscriptionId = resource.SubscriptionId;
                this.ManagementGroupId = resource.ManagementGroupId;
            }
        }

        /// <summary>
        /// Orchestration InstanceId
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Orchestration ExecutionId
        /// </summary>
        public string ExecutionId { get; set; }

        public string GroupId { get; set; }
        public string GroupType { get; set; }
        public string HierarchyId { get; set; }
        public string RootId { get; set; }
        public string DeploymentId { get; set; }
        public string CorrelationId { get; set; }

        public string SubscriptionId { get; set; }
        public string ManagementGroupId { get; set; }

        /// <summary>
        /// child of copy, the ParentId copy's path, like /Microsoft.Resources/deployments/{deploymentName}copy/{copyname}
        /// </summary>
        public string ParentResourceId { get; set; }

        public string ResourceId { get; set; }

        /// <summary>
        /// the name of resource
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The serviceType of resource
        /// </summary>
        public string Type { get; set; }

        public ProvisioningStage Stage { get; set; }

        /// <summary>
        /// the input to provisioning the resource
        /// </summary>
        public string Input { get; set; }

        /// <summary>
        /// the result of provisioning the resource
        /// </summary>
        public string Result { get; set; }

        public string Comments { get; set; }

        /// <summary>
        /// the user id of create this deployment
        /// </summary>
        public string CreateByUserId { get; set; }

        /// <summary>
        /// the user Id of last run this deployment
        /// </summary>
        public string LastRunUserId { get; set; }
    }
}