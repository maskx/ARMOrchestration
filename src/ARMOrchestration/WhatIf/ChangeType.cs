namespace maskx.ARMOrchestration.WhatIf
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-deploy-what-if#change-types"/>
    ///<seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#changetype"/>
    public enum ChangeType
    {
        /// <summary>
        /// The resource doesn't currently exist but is defined in the template. The resource will be created.
        /// </summary>
        Create,

        /// <summary>
        /// This change type only applies when using complete mode for deployment. The resource exists, but isn't defined in the template. With complete mode, the resource will be deleted. Only resources that support complete mode deletion are included in this change type.
        /// </summary>
        Delete,

        /// <summary>
        /// The resource exists, but isn't defined in the template. The resource won't be deployed or modified.
        /// </summary>
        Ignore,

        /// <summary>
        /// The resource exists, and is defined in the template. The resource will be redeployed, but the properties of the resource won't change. This change type is returned when ResultFormat is set to FullResourcePayloads, which is the default value.
        /// </summary>
        NoChange,

        /// <summary>
        /// The resource exists, and is defined in the template. The resource will be redeployed, and the properties of the resource will change. This change type is returned when ResultFormat is set to FullResourcePayloads, which is the default value.
        /// </summary>
        Modify,

        /// <summary>
        /// The resource exists, and is defined in the template. The resource will be redeployed. The properties of the resource may or may not change. The operation returns this change type when it doesn't have enough information to determine if any properties will change. You only see this condition when ResultFormat is set to ResourceIdOnly.
        /// </summary>
        Deploy
    }
}