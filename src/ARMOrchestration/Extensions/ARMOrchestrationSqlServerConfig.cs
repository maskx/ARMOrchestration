using maskx.DurableTask.SQLServer.Settings;
using maskx.OrchestrationService.Extensions;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Extensions
{
    public class ARMOrchestrationSqlServerConfig : ARMOrchestrationConfig
    {
        public DatabaseConfig Database { get; set; }
        public SQLServerOrchestrationServiceSettings OrchestrationServiceSettings { get; set; } = new SQLServerOrchestrationServiceSettings();
    }
}