using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;

namespace maskx.ARMOrchestration
{
    public class DeploymentOperation
    {
        public DeploymentOperation()
        {
        }

        public DeploymentOperation(DeploymentContext deploymentContext, IInfrastructure infrastructure, Resource resource = null)
        {
            this.GroupType = deploymentContext.GroupType;
            this.GroupId = deploymentContext.GroupId;
            this.HierarchyId = deploymentContext.HierarchyId;
            this.RootId = deploymentContext.RootId;
            this.DeploymentId = deploymentContext.DeploymentId;
            this.CorrelationId = deploymentContext.CorrelationId;
            if (resource == null)
            {
                if (!string.IsNullOrEmpty(deploymentContext.SubscriptionId))
                {
                    this.SubscriptionId = deploymentContext.SubscriptionId;
                    this.ResourceId = $"/{infrastructure.BuiltinPathSegment.Subscription}/{deploymentContext.SubscriptionId}";
                }
                if (!string.IsNullOrEmpty(deploymentContext.ManagementGroupId))
                {
                    this.ManagementGroupId = deploymentContext.ManagementGroupId;
                    this.ResourceId = $"/{infrastructure.BuiltinPathSegment.ManagementGroup}/{deploymentContext.ManagementGroupId}";
                }
                if (!string.IsNullOrEmpty(deploymentContext.ResourceGroup))
                    this.ResourceId += $"/{infrastructure.BuiltinPathSegment.ResourceGroup}/{deploymentContext.ResourceGroup}";
                this.ResourceId += $"/{infrastructure.BuiltinPathSegment.Provider}/{infrastructure.BuitinServiceTypes.Deployments}/{deploymentContext.DeploymentName}";

                this.Type = infrastructure.BuitinServiceTypes.Deployments;
                this.Name = deploymentContext.DeploymentName;
                this.ParentResourceId = deploymentContext.ParentId;
            }
            else
            {
                this.ResourceId = resource.ResouceId;
                this.Name = resource.Name;
                this.Type = resource.Type;
                this.ParentResourceId = string.IsNullOrEmpty(resource.CopyId) ? deploymentContext.DeploymentId : resource.CopyId;
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
        /// child of copy, the ParentId copy's path, like /Microsoft.Resources/deployments/copy/{copyname}
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
    }
}