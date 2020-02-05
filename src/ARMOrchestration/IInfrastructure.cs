using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration
{
    public interface IInfrastructure
    {
        AsyncRequestInput GetRequestInput(RequestOrchestrationInput input);

        TaskResult List(DeploymentContext context, string resourceId, string apiVersion, string functionValues = "", string value = "");

        TaskResult Reference(DeploymentContext context, string resourceName, string apiVersion = "", bool full = false);

        BuiltinServiceTypes BuitinServiceTypes { get; set; }
        List<string> ExtensionResources { get; set; }
    }
}