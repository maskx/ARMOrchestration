using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService.Extensions;
using System;

namespace maskx.ARMOrchestration.Extensions
{
    public class ARMOrchestrationConfig
    {
        /// <summary>
        /// indicate whether include the exception details in task events
        /// </summary>
        public bool IncludeDetails { get; set; } = false;

        public Action<ARMFunctions> ConfigARMFunctions { get; set; }
        public OrchestrationWorkerOptions OrchestrationWorkerOptions { get; set; } = new OrchestrationWorkerOptions();
        public CommunicationWorkerOptions CommunicationWorkerOptions { get; set; } = new CommunicationWorkerOptions();
    }
}