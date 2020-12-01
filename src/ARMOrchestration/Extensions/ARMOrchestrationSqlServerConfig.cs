using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService.Worker;
using System;

namespace maskx.ARMOrchestration.Extensions
{
    public class ARMOrchestrationSqlServerConfig 
    {
        public DatabaseConfig Database { get; set; }
        public bool IncludeDetails { get; set; } = false;

        public Action<ARMFunctions> ConfigARMFunctions { get; set; }
        // todo: 重新定义类，去除数据库配置项，统一使用ARMOrchestrationSqlServerConfig.DataBase的配置项
        public OrchestrationWorkerOptions OrchestrationWorkerOptions { get; set; } = new OrchestrationWorkerOptions();
        // todo: 重新定义类，去除数据库配置项，统一使用ARMOrchestrationSqlServerConfig.DataBase的配置项
        public CommunicationWorkerOptions CommunicationWorkerOptions { get; set; } = new CommunicationWorkerOptions();
    }
}