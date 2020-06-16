namespace maskx.ARMOrchestration
{
    /// <summary>
    /// OnlyCreation mode will only create new resource, if a resource already exist, an error is reported
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deployment-modes"/>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#deploymentmode"/>
    public enum DeploymentMode
    {
        Complete,
        Incremental,
        OnlyCreation
    }
}