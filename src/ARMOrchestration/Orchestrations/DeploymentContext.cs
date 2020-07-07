using maskx.ARMOrchestration.ARMTemplate;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentContext
    {
        /// <summary>
        /// group Id
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// group type, such as resource group, subscription, management group
        /// </summary>
        public string GroupType { get; set; }

        /// <summary>
        /// include the information from root to this one
        /// </summary>
        public string HierarchyId { get; set; }

        /// <summary>
        /// the deploymentId of root deployment
        /// </summary>
        public string RootId { get; set; }

        /// <summary>
        /// the deploymentId of parent, DependsOn will search resource status in this deployment's scope
        /// </summary>
        public string ParentId { get; set; }

        public string DeploymentId { get; set; }
        public string CorrelationId { get; set; }

        /// <summary>
        /// Deployment Name
        /// </summary>
        public string DeploymentName { get; set; }

        public string ResourceGroup { get; set; }
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ManagementGroupId { get; set; }

        /// <summary>
        /// Complete  or Incremental
        /// </summary>
        public DeploymentMode Mode { get; set; } = DeploymentMode.Incremental;

        public Template Template { get; set; }
        public string Parameters { get; set; }
        public string ApiVersion { get; set; }

        /// <summary>
        /// When a deployment fails, you can automatically redeploy an earlier, successful deployment from your deployment history. This functionality is useful if you've got a known good state for your infrastructure deployment and want to revert to this state.
        /// </summary>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/rollback-on-error"/>
        public bool RollbackToLastDeployment { get; set; } = false;

        /// <summary>
        /// the user id of create this deployment
        /// </summary>
        public string CreateByUserId { get; set; }

        /// <summary>
        /// the user Id of last run this deployment
        /// </summary>
        public string LastRunUserId { get; set; }

        /// <summary>
        /// some extension settings
        /// </summary>
        public Dictionary<string, object> Extensions { get; set; }

        public TemplateLink TemplateLink { get; set; }
        public ParametersLink ParametersLink { get; set; }
        public string TemplateContent { get; set; }
        public string GetResourceId(IInfrastructure infrastructure)
        {
            string resourceId = string.Empty;
            if (!string.IsNullOrEmpty(this.SubscriptionId))
                resourceId = $"/{infrastructure.BuiltinPathSegment.Subscription}/{this.SubscriptionId}";
            if (!string.IsNullOrEmpty(this.ManagementGroupId))
                resourceId = $"/{infrastructure.BuiltinPathSegment.ManagementGroup}/{this.ManagementGroupId}";
            if (!string.IsNullOrEmpty(this.ResourceGroup))
                resourceId += $"/{infrastructure.BuiltinPathSegment.ResourceGroup}/{this.ResourceGroup}";
            resourceId += $"/{infrastructure.BuiltinPathSegment.Provider}/{infrastructure.BuitinServiceTypes.Deployments}/{this.DeploymentName}";
            return resourceId;
        }
    }
}