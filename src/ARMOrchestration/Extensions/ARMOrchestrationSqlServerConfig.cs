using maskx.DurableTask.SQLServer.Settings;
using maskx.OrchestrationService.Extensions;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Extensions
{
    public class ARMOrchestrationSqlServerConfig
    {
        public DatabaseConfig Database { get; set; }
        public SQLServerOrchestrationServiceSettings OrchestrationServiceSettings { get; set; } = new SQLServerOrchestrationServiceSettings();
        public OrchestrationWorkerOptions OrchestrationWorkerOptions { get; set; } = new OrchestrationWorkerOptions();
        public CommunicationWorkerOptions CommunicationWorkerOptions { get; set; } = new CommunicationWorkerOptions();
        public GetRequestInput GetRequestInput { get; set; }
        public List<string> ExtensionResources { get; set; } = new List<string>();
        public BuiltinServiceTypes BuitinServiceTypes { get; set; } = new BuiltinServiceTypes();
    }
}