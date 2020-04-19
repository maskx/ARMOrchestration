namespace maskx.ARMOrchestration.Activities
{
    public enum ProvisioningStage
    {
        Failed = -1400,
        ChildResourceFailed = -1100,
        ExtensionResourceFailed = -1000,
        ResourceCreateFailed = -700,
        DependsOnWaited = -200,
        ConditionCheckFailed = -100,
        StartProcessing = 0,
        ConditionCheckSuccessed = 100,
        DependsOnSuccessed = 200,
        ResourceCreateSuccessed = 700,
        ExtensionResourceSuccessed = 1000,
        ChildResourceSuccessed = 1100,
        Successed = 1400
    }
}