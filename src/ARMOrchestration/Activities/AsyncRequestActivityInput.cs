namespace maskx.ARMOrchestration.Activities
{
    public class AsyncRequestActivityInput
    {
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public ProvisioningStage ProvisioningStage { get; set; }
    }
}