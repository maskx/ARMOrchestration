using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration
{
    public delegate AsyncRequestInput GetRequestInput(IServiceProvider serviceProvider, DeploymentContext context, Resource resource, string name, string property);

    public delegate TaskResult ListFunction(string resourceId, string apiVersion, string functionValues = "", string value = "");

    public class ARMOrchestrationOptions
    {
        /// <summary>
        /// Idel time when no dependsOn resource completed
        /// </summary>
        public int DependsOnIdelMilliseconds { get; set; } = 500;

        public DatabaseConfig Database { get; set; }
        public ListFunction ListFunction { get; set; }
        public GetRequestInput GetRequestInput { get; set; }
        public List<string> ExtensionResources { get; set; } = new List<string>();
        public BuitinServiceTypes BuitinServiceTypes { get; set; } = new BuitinServiceTypes();
    }
}