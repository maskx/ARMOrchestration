using System.Collections.Generic;

namespace maskx.ARMOrchestration.WhatIf
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#whatifchange"/>
    public class WhatIfChange
    {
        public string After { get; set; }
        public string Before { get; set; }
        public ChangeType ChangeType { get; set; }
        public List<WhatIfPropertyChange> Delta { get; set; }
        public string ResourceId { get; set; }
    }
}