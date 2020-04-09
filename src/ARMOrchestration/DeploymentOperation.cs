using maskx.ARMOrchestration.Activities;

namespace maskx.ARMOrchestration
{
    public class DeploymentOperation
    {
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
    }
}