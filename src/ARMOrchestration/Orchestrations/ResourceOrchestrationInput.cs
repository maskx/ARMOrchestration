using System.Collections.Generic;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestrationInput
    {
        public string DeploymentId { get; set; }
        public string CorrelationId { get; set; }
        public ParentResource Parent { get; set; }
        public string Resource { get; set; }
        public Dictionary<string, object> OrchestrationContext { get; set; }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/child-resource-name-type
        /// when a resource in a copy loop,the parent resource is the loop.
        /// Resource={loopname}
        /// Type= copy
        /// ResourceId=deployment/{deploymentId}/copy/{loopname}
        /// </summary>
        public class ParentResource
        {
            public string Resource { get; set; }
            public string Type { get; set; }
            public string ResourceId { get; set; }
        }
    }
}