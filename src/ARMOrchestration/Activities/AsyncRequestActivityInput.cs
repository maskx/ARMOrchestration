namespace maskx.ARMOrchestration.Activities
{
    public class AsyncRequestActivityInput
    {
        public string DeploymentOperationId { get; set; }
        public ProvisioningStage ProvisioningStage { get; set; }
    }
}