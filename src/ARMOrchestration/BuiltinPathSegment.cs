using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.ARMOrchestration
{
    public class BuiltinPathSegment
    {
        public string Subscription { get; set; } = "subscription";
        public string ManagementGroup { get; set; } = "management";
        public string ResourceGroup { get; set; } = "resourceGroups";
        public string Provider { get; set; } = "providers";
    }
}