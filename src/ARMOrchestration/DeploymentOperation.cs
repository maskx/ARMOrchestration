using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using Microsoft.Extensions.DependencyInjection;

namespace maskx.ARMOrchestration
{
    public class DeploymentOperation
    {
        public DeploymentOperation()
        {
        }
        public DeploymentOperation(Deployment deployment)
        {
            BuildDeploymentInformation(deployment);
            IInfrastructure infrastructure = deployment.ServiceProvider.GetService<IInfrastructure>();
            this.ResourceId = deployment.ResourceId;
            this.Type = infrastructure.BuiltinServiceTypes.Deployments;
            this.Name = deployment.Name;
            this.ParentResourceId = deployment.ParentId;
            this.SubscriptionId = deployment.SubscriptionId;
            this.ManagementGroupId = deployment.ManagementGroupId;
        }
        private void BuildDeploymentInformation(Deployment deployment)
        {
            this.GroupType = deployment.GroupType;
            this.GroupId = deployment.GroupId;
            this.HierarchyId = deployment.HierarchyId;
            this.RootId = deployment.RootId;
            this.DeploymentId = deployment.DeploymentId;
            this.CorrelationId = deployment.CorrelationId;
            this.CreateByUserId = deployment.CreateByUserId;
            if (string.IsNullOrEmpty(deployment.LastRunUserId))
                this.LastRunUserId = this.CreateByUserId;
            else
                this.LastRunUserId = deployment.LastRunUserId;

        }
        public DeploymentOperation(Resource resource)
        {
            BuildDeploymentInformation(resource.Input);
            this.ResourceId = resource.ResourceId;
            this.Name = resource.Name;
            this.Type = (resource.Copy != null && !resource.CopyIndex.HasValue) ? resource.Copy.Type : resource.Type;
            this.ParentResourceId = resource.CopyIndex.HasValue ? resource.Copy.Id : resource.Input.ResourceId;
            this.SubscriptionId = resource.SubscriptionId;
            this.ManagementGroupId = resource.ManagementGroupId;
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