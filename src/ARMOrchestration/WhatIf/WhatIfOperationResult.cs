using maskx.ARMOrchestration.Orchestrations;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.WhatIf
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#whatifoperationresult"/>
    public class WhatIfOperationResult
    {
        public ErrorResponse Error { get; set; }
        public List<WhatIfChange> Changes { get; set; } = new List<WhatIfChange>();
        public string Status { get; set; }
    }
}