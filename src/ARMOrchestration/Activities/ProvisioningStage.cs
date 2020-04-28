namespace maskx.ARMOrchestration.Activities
{
    public enum ProvisioningStage
    {
        // begin deployment special

        InjectBeforeDeployment = 100,
        BeforeDeployment = 200,
        ValidateTemplate = 300,

        // end deployment special

        DependsOnWaited = 400,

        // begin resource special

        InjectBefroeProvisioning = 500,
        BeforeResourceProvisioning = 600,
        ProvisioningResource = 700,
        WaitChildCompleted = 800,
        CreateExtensionResource = 900,
        AfterResourceProvisioningOrchestation = 1000,
        InjectAfterProvisioning = 1100,

        // end resource special

        // begin deployment special
        AfterDeploymentOrhcestration = 1200,

        InjectAfterDeployment = 1300,
        // end deployment special

        Successed = 1400
    }
}