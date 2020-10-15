namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/templates/microsoft.resources/deployments#deploymentproperties-object
    /// </summary>
    public class DeploymentProperty
    {
        public Template Template { get; set; }
        public string Parameters { get; set; }
        public TemplateLink TemplateLink { get; set; }
        public ParametersLink ParametersLink { get; set; }
        public DeploymentMode Mode { get; set; }
        public DebugSetting DebugSetting { get; set; }
        public OnErrorDeployment OnErrorDeployment { get; set; }
    }
}
