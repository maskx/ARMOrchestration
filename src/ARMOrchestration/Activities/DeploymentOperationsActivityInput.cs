namespace maskx.ARMOrchestration.Activities
{
    public partial class DeploymentOperationsActivityInput
    {
        public string DeploymentId { get; set; }
        public string ParentId { get; set; }
        public string InstanceId { get; set; }
        public string ExecutionId { get; set; }
        public string ResourceId { get; set; }
        public string Resource { get; set; }
        public string Type { get; set; }
        public ProvisioningStage Stage { get; set; }
        public string CorrelationId { get; set; }
        public string Result { get; set; }
    }
}