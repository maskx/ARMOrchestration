namespace maskx.ARMOrchestration.WhatIf
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-deploy-what-if#change-types"/>
    ///<seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#changetype"/>
    public enum ChangeTypeEnum
    {
        Create,
        Delete,
        Ignore,
        NoChange,
        Modify,
        Deploy
    }
}