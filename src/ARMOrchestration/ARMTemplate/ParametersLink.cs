namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#parameterslink"/>
    /// <seealso cref="https://docs.microsoft.com/en-us/azure/templates/microsoft.resources/deployments#ParametersLink"/>
    public class ParametersLink
    {
        public string Uri { get; set; }
        public string ContentVersion { get; set; }
    }
}