using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration
{
    public delegate AsyncRequestInput GetRequestInput(IServiceProvider serviceProvider, RequestOrchestrationInput input);

    public delegate TaskResult ListFunction(IServiceProvider serviceProvider, DeploymentContext context, string resourceId, string apiVersion, string functionValues = "", string value = "");

    public delegate TaskResult ReferenceFunction(IServiceProvider serviceProvider, DeploymentContext context, string resourceName, string apiVersion = "", bool full = false);

    public class ARMOrchestrationOptions
    {
        /// <summary>
        /// Idel time when no dependsOn resource completed
        /// </summary>
        public int DependsOnIdelMilliseconds { get; set; } = 500;

        public DatabaseConfig Database { get; set; }
        public ListFunction ListFunction { get; set; }
        public ReferenceFunction ReferenceFunction { get; set; }
        public GetRequestInput GetRequestInput { get; set; }
        public List<string> ExtensionResources { get; set; } = new List<string>();
        public BuiltinServiceTypes BuitinServiceTypes { get; set; } = new BuiltinServiceTypes();
    }
}