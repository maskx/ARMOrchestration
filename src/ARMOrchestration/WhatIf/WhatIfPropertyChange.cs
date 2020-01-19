using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.ARMOrchestration.WhatIf
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/rest/api/resources/deployments/whatif#whatifpropertychange"/>
    public class WhatIfPropertyChange
    {
        public string After { get; set; }
        public string Before { get; set; }
        public List<WhatIfPropertyChange> Children { get; set; }
        public string Path { get; set; }
        public PropertyChangeType PropertyChangeType { get; set; }
    }
}