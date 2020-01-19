using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.ARMOrchestration.Orchestrations
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deployment-modes"/>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#deploymentmode"/>
    public enum DeploymentModeEnum
    {
        Complete,
        Incremental
    }
}