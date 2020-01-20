using maskx.DurableTask.SQLServer.Settings;
using maskx.OrchestrationService.Extensions;

namespace maskx.ARMOrchestration.Extensions
{
    public class ARMOrchestrationSqlServerConfig
    {
        public bool AutoCreate { get; set; } = false;
        public string ConnectionString { get; set; }
        public string HubName { get; set; } = "ARM";
        public string SchemaName { get; set; } = "dbo";
        public SQLServerOrchestrationServiceSettings OrchestrationServiceSettings { get; set; } = new SQLServerOrchestrationServiceSettings();
        public OrchestrationWorkerOptions OrchestrationWorkerOptions { get; set; }
        public CommunicationWorkerOptions CommunicationWorkerOptions { get; set; }
    }
}