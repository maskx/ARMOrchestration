using maskx.DurableTask.SQLServer.Settings;
using System.Collections.Generic;

namespace maskx.ARMOrchestration
{
    public class ARMOrchestrationOptions
    {
        public string ConnectionString { get; set; }
        public string HubName { get; set; } = "ARM";
        public SQLServerOrchestrationServiceSettings OrchestrationServiceSettings { get; set; } = new SQLServerOrchestrationServiceSettings();
        public Dictionary<string, string> ExtensionResources { get; set; }
    }
}