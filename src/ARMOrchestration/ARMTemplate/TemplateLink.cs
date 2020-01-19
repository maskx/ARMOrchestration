using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#templatelink"/>
    public class TemplateLink
    {
        public string Uri { get; set; }
        public string ContentVersion { get; set; }
    }
}