using System.Collections.Generic;

namespace maskx.ARMOrchestration.Activities
{
    public class WaitDependsOnActivityInput
    {
        public string RootId { get; set; }
        public string DeploymentId { get; set; }
        public string InstanceId { get; set; }
        public List<string> DependsOn { get; set; }
    }
}