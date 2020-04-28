using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using System.Collections.Generic;

namespace maskx.ARMOrchestration
{
    public interface IInfrastructure
    {
        AsyncRequestInput GetRequestInput(AsyncRequestActivityInput input);

        TaskResult List(DeploymentContext context, string resourceId, string apiVersion, string functionValues = "", string value = "");

        TaskResult Reference(DeploymentContext context, string resourceName, string apiVersion = "", bool full = false);

        TaskResult WhatIf(DeploymentContext context, string resourceName);

        BuiltinServiceTypes BuitinServiceTypes { get; set; }
        BuiltinPathSegment BuiltinPathSegment { get; set; }
        List<string> ExtensionResources { get; set; }
        bool InjectBeforeDeployment { get; set; }
        bool InjectAfterDeployment { get; set; }
        bool InjectBefroeProvisioning { get; set; }
        bool InjectAfterProvisioning { get; set; }

        /// <summary>
        /// The Orchestration should be register in OrchestartionService
        /// should inherit TaskOrchestration<TaskResult, DeploymentOrchestrationInput>
        /// </summary>
        List<(string Name, string Version)> BeforeDeploymentOrchestration { get; set; }

        /// <summary>
        /// The Orchestration should be register in OrchestartionService
        /// should inherit TaskOrchestration<TaskResult, DeploymentOrchestrationInput>
        /// </summary>
        List<(string Name, string Version)> AfterDeploymentOrhcestration { get; set; }

        /// <summary>
        /// The Orchestration should be register in OrchestartionService
        /// should inherit TaskOrchestration<TaskResult, ResourceOrchestrationInput>
        /// </summary>
        List<(string Name, string Version)> BeforeResourceProvisioningOrchestation { get; set; }

        /// <summary>
        /// The Orchestration should be register in OrchestartionService
        /// should inherit TaskOrchestration<TaskResult, ResourceOrchestrationInput>
        /// </summary>
        List<(string Name, string Version)> AfterResourceProvisioningOrchestation { get; set; }
    }
}