using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#parameterslink"/>
    public class ParametersLink
    {
        public string Uri { get; set; }
        public string ContentVersion { get; set; }
    }
}