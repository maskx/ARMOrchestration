namespace maskx.ARMOrchestration.WhatIf
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-deploy-what-if#result-format"/>
    ///<seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#whatifresultformat"/>
    public enum WhatIfResultFormat
    {
        FullResourcePayloads,
        ResourceIdOnly
    }
}