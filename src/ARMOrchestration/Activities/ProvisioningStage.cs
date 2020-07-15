namespace maskx.ARMOrchestration.Activities
{
    public enum ProvisioningStage
    {
        Pending = 0,
        // begin deployment special
        
        ValidateTemplate = 100,
        ValidateTemplateFailed = -100,

        InjectBeforeDeployment = 200,
        InjectBeforeDeploymentFailed = -200,

        BeforeDeployment = 300,
        BeforeDeploymentFailed = -300,

        // end deployment special

        DependsOnWaited = 400,
        DependsOnWaitedFailed = -400,

        // begin resource special

        ExpandResourceProperties=500,
        ExpandResourcePropertiesFailed=-500,

        InjectBefroeProvisioning = 600,
        InjectBefroeProvisioningFailed = -600,

        BeforeResourceProvisioning = 700,
        BeforeResourceProvisioningFailed = -700,

        ProvisioningResource = 800,
        ProvisioningResourceFailed = -800,

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