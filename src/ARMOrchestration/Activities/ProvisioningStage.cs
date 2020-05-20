namespace maskx.ARMOrchestration.Activities
{
    public enum ProvisioningStage
    {
        // begin deployment special

        InjectBeforeDeployment = 100,
        InjectBeforeDeploymentFailed = -100,

        BeforeDeployment = 200,
        BeforeDeploymentFailed = -200,

        ValidateTemplate = 300,
        ValidateTemplateFailed = -300,

        // end deployment special

        DependsOnWaited = 400,
        DependsOnWaitedFailed = -400,

        // begin resource special

        InjectBefroeProvisioning = 500,
        InjectBefroeProvisioningFailed = -500,

        BeforeResourceProvisioning = 600,
        BeforeResourceProvisioningFailed = -600,

        ProvisioningResource = 700,
        ProvisioningResourceFailed = -700,

        WaitChildCompleted = 800,
        WaitChildCompletedFailed = -800,

        CreateExtensionResource = 900,
        CreateExtensionResourceFailed = -900,

        AfterResourceProvisioningOrchestation = 1000,
        AfterResourceProvisioningOrchestationFailed = -1000,

        InjectAfterProvisioning = 1100,
        InjectAfterProvisioningFailed = -1100,

        // end resource special

        // begin deployment special
        AfterDeploymentOrhcestration = 1200,

        AfterDeploymentOrhcestrationFailed = -1200,

        InjectAfterDeployment = 1300,
        InjectAfterDeploymentFailed = -1300,
        // end deployment special

        Successed = 1400,
        Failed = -1400
    }
}