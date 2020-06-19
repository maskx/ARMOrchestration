using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using System.Collections.Generic;

namespace maskx.ARMOrchestration
{
    /// <summary>
    /// The bridge of ARMOrchestration to On-premises Infrastructure
    /// </summary>
    public interface IInfrastructure
    {
      //  void ValidateResource(string resource, DeploymentContext context);
        /// <summary>
        /// Convert the message to the define format of communication table
        /// </summary>
        /// <param name="input">AsyncRequestActivityInput</param>
        /// <returns>AsyncRequestInput</returns>
        AsyncRequestInput GetRequestInput(AsyncRequestActivityInput input);

        /// <summary>
        /// <see cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#list"/>
        /// </summary>
        /// <param name="context">Deployment Context</param>
        /// <param name="resourceId">Unique identifier for the resource.</param>
        /// <param name="apiVersion">API version of resource runtime state. Typically, in the format, yyyy-mm-dd.</param>
        /// <param name="functionValues">An object that has values for the function. Only provide this object for functions that support receiving an object with parameter values</param>
        /// <param name="value"></param>
        /// <returns></returns>
        TaskResult List(DeploymentContext context, string resourceId, string apiVersion, string functionValues = "", string value = "");

        /// <summary>
        /// <see cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#reference"/>
        /// </summary>
        /// <param name="context">
        /// Deployment Context
        /// </param>
        /// <param name="resourceName">
        /// Name or unique identifier of a resource. When referencing a resource in the current template, provide only the resource name as a parameter. When referencing a previously deployed resource or when the name of the resource is ambiguous, provide the resource ID.
        /// </param>
        /// <param name="apiVersion">
        /// API version of the specified resource. This parameter is required when the resource isn't provisioned within same template. Typically, in the format, yyyy-mm-dd. For valid API versions for your resource, see template reference.
        /// </param>
        /// <param name="full">
        /// Value that specifies whether to return the full resource object. If you don't specify 'Full', only the properties object of the resource is returned. The full object includes values such as the resource ID and location.
        /// </param>
        /// <returns>
        /// an object representing a resource's runtime state
        /// </returns>
        TaskResult Reference(DeploymentContext context, string resourceName, string apiVersion = "", bool full = false);

        TaskResult WhatIf(DeploymentContext context, string resourceName);

        /// <summary>
        /// <see cref="https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#providers"/>
        /// </summary>
        /// <param name="providerNamespace">Namespace of the provider</param>
        /// <param name="resourceType">
        /// The type of resource within the specified namespace.
        /// If you don't provide a resource type, the function returns all the supported types for the resource provider
        /// </param>
        /// <returns>
        /// Information about a resource provider and its supported resource types
        /// </returns>
        TaskResult Providers(string providerNamespace, string resourceType);

        BuiltinServiceTypes BuitinServiceTypes { get; set; }
        BuiltinPathSegment BuiltinPathSegment { get; set; }

        /// <summary>
        /// Inject a request to communicationworker at the begin of deployment
        /// </summary>
        bool InjectBeforeDeployment { get; set; }

        /// <summary>
        /// Inject a request to communicationworker at the end of deployment
        /// </summary>
        bool InjectAfterDeployment { get; set; }

        /// <summary>
        /// Inject a request to communicationworker at the begin of provisioning a resource
        /// </summary>
        bool InjectBefroeProvisioning { get; set; }

        /// <summary>
        /// Inject a request to communicationworker at the end of provisioning a resource
        /// </summary>
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