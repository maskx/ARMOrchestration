using maskx.DurableTask.SQLServer.Settings;

namespace maskx.ARMOrchestration.Extensions
{
    public class ARMOrchestrationConfig
    {
        public string ConnectionString { get; set; }
        public string HubName { get; set; } = "ARM";
        public SQLServerOrchestrationServiceSettings OrchestrationServiceSettings { get; set; } = new SQLServerOrchestrationServiceSettings();
    }
}