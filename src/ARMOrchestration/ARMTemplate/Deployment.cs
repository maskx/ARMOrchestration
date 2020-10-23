using Newtonsoft.Json;
using System;

namespace maskx.ARMOrchestration.ARMTemplate
{
    ///// <summary>
    ///// https://docs.microsoft.com/en-us/azure/templates/microsoft.resources/deployments#microsoftresourcesdeployments-object
    ///// </summary>
    //[JsonObject(MemberSerialization.OptOut)]
    //public class Deployment
    //{
    //    /// <summary>
    //    /// Deployment Name
    //    /// </summary>
    //    public string Name { get; set; }
    //    public string Type { get; set; }
    //    public string ApiVersion { get; set; }
    //    /// <summary>
    //    /// The location to store the deployment data
    //    /// </summary>
    //    public string Location { get; set; }
    //    /// <summary>
    //    /// In tenant and management group deployments, provide the management group ID to target. Use the format Microsoft.Management/managementGroups/{managementGroupID}
    //    /// </summary>
    //    public string Scope { get; set; }
    //    public Guid SubscriptionId { get; set; }
    //    public string ResourceGroup { get; set; }
    //}
    ///// <summary>
    ///// https://docs.microsoft.com/en-us/azure/templates/microsoft.resources/deployments#deploymentproperties-object
    ///// https://docs.microsoft.com/en-us/rest/api/resources/deployments/createorupdate#deploymentproperties
    ///// </summary>
    //public class DeploymentProperty
    //{
    //    public Template Template { get; set; }
    //    public string Parameters { get; set; }
    //    public TemplateLink TemplateLink { get; set; }
    //    public ParametersLink ParametersLink { get; set; }
    //    public DeploymentMode Mode { get; set; }
    //    public DebugSetting DebugSetting { get; set; }
    //    public OnErrorDeployment OnErrorDeployment { get; set; }
    //    public ExpressionEvaluationOption ExpressionEvaluationOptions { get; set; }
    //}
    //public class ExpressionEvaluationOption
    //{
    //    public string Scope { get; set; }
    //}
}
