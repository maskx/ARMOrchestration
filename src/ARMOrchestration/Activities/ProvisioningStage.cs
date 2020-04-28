namespace maskx.ARMOrchestration.Activities
{
    public enum ProvisioningStage
    {
        // deployment special
        InjectBeforeDeployment = 100,

        BeforeDeployment = 200,
        ValidateTemplate = 300,
        // deployment special

        DependsOnWaited = 400,

        // resource special
        InjectBefroeProvisioning = 500,

        BeforeResourceProvisioning = 600,
        ProvisioningResource = 700,
        WaitChildCompleted = 800,
        CreateExtensionResource = 900,
        AfterResourceProvisioningOrchestation = 1000,
        InjectAfterProvisioning = 1100,
        // resource special

        // deployment special
        AfterDeploymentOrhcestration = 1200,

        InjectAfterDeployment = 1300,
        // deployment special

        Successed = 1400
    }
}