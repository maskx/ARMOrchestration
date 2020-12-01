namespace maskx.ARMOrchestration.ARMTemplate
{
    public class OnErrorDeployment
    {
        /// <summary>
        /// The deployment on error behavior type. Possible values are LastSuccessful and SpecificDeployment
        /// LastSuccessful： will redeploy the last successful deployment if the current deployment fails
        /// SpecificDeployment: must set the DeploymentName
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// The deployment to be used on error case.
        /// </summary>
        public string DeploymentName { get; set; }
    }
}
