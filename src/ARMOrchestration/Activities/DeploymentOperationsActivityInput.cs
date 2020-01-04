namespace maskx.ARMOrchestration.Activities
{
    public class DeploymentOperationsActivityInput
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

        public enum ProvisioningStage
        {
            Failed = -130,
            ChildResourceFailed = -120,
            PolicyApplyFailed = -110,
            ExtensionResourceFailed = -100,
            ResourceCommitFailed = -90,
            QuotaCommitFailed = -80,
            ResourceCreateFailed = -70,
            QuotaCheckFailed = -60,
            LockCheckFailed = -50,
            ResourceCheckFailed = -40,
            PolicyCheckFailed = -30,
            DependsOnWaited = -20,
            ConditionCheckFailed = -10,
            StartProcessing = 0,
            ConditionCheckSuccessed = 10,
            DependsOnSuccessed = 20,
            PolicyCheckSuccessed = 30,
            ResourceCheckSuccessed = 40,
            LockCheckSuccessed = 50,
            QuotaCheckSuccessed = 60,
            ResourceCreateSuccessed = 70,
            QuotaCommitSuccesed = 80,
            ResourceCommitSuccessed = 90,
            ExtensionResourceSuccessed = 100,
            PolicyApplySuccessed = 110,
            ChildResourceSuccessed = 120,
            Successed = 130
        }
    }
}