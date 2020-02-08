namespace maskx.ARMOrchestration.Activities
{
    public enum ProvisioningStage
    {
        Failed = -1400,
        ResourceReadyFailed = -1300,
        PolicyApplyFailed = -1200,
        ChildResourceFailed = -1100,
        ExtensionResourceFailed = -1000,
        ResourceCommitFailed = -900,
        QuotaCommitFailed = -800,
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
        QuotaCommitSuccesed = 800,
        ResourceCommitSuccessed = 900,
        ExtensionResourceSuccessed = 1000,
        ChildResourceSuccessed = 1100,
        PolicyApplySuccessed = 1200,
        ResourceReadySuccessed = 1300,
        Successed = 1400
    }
}