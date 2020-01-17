namespace maskx.ARMOrchestration.Activities
{
    public enum ProvisioningStage
    {
        Failed = -1300,
        ResourceCommitFailed = -1200,
        QuotaCommitFailed = -1100,
        PolicyApplyFailed = -1000,
        ChildResourceFailed = -900,
        ExtensionResourceFailed = -800,
        ResourceCreateFailed = -700,
        QuotaCheckFailed = -600,
        LockCheckFailed = -500,
        ResourceCheckFailed = -400,
        PolicyCheckFailed = -300,
        DependsOnWaited = -200,
        ConditionCheckFailed = -100,
        StartProcessing = 0,
        ConditionCheckSuccessed = 100,
        DependsOnSuccessed = 200,
        PolicyCheckSuccessed = 300,
        ResourceCheckSuccessed = 400,
        LockCheckSuccessed = 500,
        QuotaCheckSuccessed = 600,
        ResourceCreateSuccessed = 700,
        ExtensionResourceSuccessed = 800,
        ChildResourceSuccessed = 900,
        PolicyApplySuccessed = 1000,
        QuotaCommitSuccesed = 1100,
        ResourceCommitSuccessed = 1200,
        Successed = 1300
    }
}