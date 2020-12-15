namespace maskx.ARMOrchestration.Activities
{
    public class AsyncRequestActivityInput
    {
        public string InstanceId { get; set; }
        public string DeploymentId { get; set; }
        public ProvisioningStage ProvisioningStage { get; set; }
    }
}