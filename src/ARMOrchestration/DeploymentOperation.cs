using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;

namespace maskx.ARMOrchestration
{
    public class DeploymentOperation
    {
        public DeploymentOperation(string id)
        {
            this.Id = id;
        }
        public DeploymentOperation(string id, Deployment deployment)
        {
            BuildDeploymentInformation(id, deployment);
            IInfrastructure infrastructure = deployment.ServiceProvider.GetService<IInfrastructure>();
            this.ResourceId = deployment.ResourceId;
            this.Type = infrastructure.BuiltinServiceTypes.Deployments;
            this.Name = deployment.Name;
            this.ParentResourceId = deployment.Parent?.ResourceId;
            this.SubscriptionId = deployment.SubscriptionId;
            this.ManagementGroupId = deployment.ManagementGroupId;
        }

        public DeploymentOperation(string id, Resource resource)
        {
            BuildDeploymentInformation(id, resource.Input);
            this.ResourceId = resource.ResourceId;
            this.Name = (resource.Copy != null && !resource.CopyIndex.HasValue) ? resource.Copy.FullName : resource.Name;
            this.Type = (resource.Copy != null && !resource.CopyIndex.HasValue) ? resource.Copy.FullType : resource.Type;
            this.ParentResourceId = resource.ParentResourceId;
            this.SubscriptionId = resource.SubscriptionId;
            this.ManagementGroupId = resource.ManagementGroupId;
        }
        private void BuildDeploymentInformation(string id, Deployment deployment)
        {
            this.ApiVersion = deployment.ApiVersion;
            this.Id = id;
            this.GroupType = deployment.GroupType;
            this.GroupId = deployment.GroupId;
            this.HierarchyId = deployment.HierarchyId;
            this.RootId = deployment.RootId;
            this.DeploymentId = deployment.DeploymentId;
            this.CorrelationId = deployment.CorrelationId;
            this.CreateByUserId = deployment.CreateByUserId;
            if (string.IsNullOrEmpty(deployment.LastRunUserId))
                this.LastRunUserId = deployment.CreateByUserId;
            else
                this.LastRunUserId = deployment.LastRunUserId;

        }
        [Key]
        public string Id { get; set; }
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

        public ProvisioningStage Stage { get; set; } = ProvisioningStage.Pending;

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
        public DateTime CreateTimeUtc { get; set; }
        public DateTime UpdateTimeUtc { get; set; }
        public string ApiVersion { get; set; }
    }
}