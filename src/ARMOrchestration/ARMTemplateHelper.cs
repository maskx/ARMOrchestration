using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.ARMOrchestration.WhatIf;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace maskx.ARMOrchestration
{
    public class ARMTemplateHelper
    {
        private readonly ARMOrchestrationOptions options;
        public readonly ARMFunctions ARMfunctions;
        public readonly IInfrastructure infrastructure;

        private readonly string _saveDeploymentOperationCommandString;

        public ARMTemplateHelper(
            IOptions<ARMOrchestrationOptions> options,
            ARMFunctions functions,
            IInfrastructure infrastructure)
        {
            this.options = options?.Value;
            this.ARMfunctions = functions;
            this.infrastructure = infrastructure;

            this._saveDeploymentOperationCommandString = string.Format(@"
MERGE {0} with (serializable) [Target]
USING (VALUES (@InstanceId,@ExecutionId)) as [Source](InstanceId,ExecutionId)
ON [Target].InstanceId = [Source].InstanceId AND [Target].ExecutionId = [Source].ExecutionId
WHEN NOT MATCHED THEN
	INSERT
	([InstanceId],[ExecutionId],[GroupId],[GroupType],[HierarchyId],[RootId],[DeploymentId],[CorrelationId],[ParentResourceId],[ResourceId],[Name],[Type],[Stage],[CreateTimeUtc],[UpdateTimeUtc],[SubscriptionId],[ManagementGroupId],[Input],[Result],[Comments],[CreateByUserId],[LastRunUserId])
	VALUES
	(@InstanceId,@ExecutionId,@GroupId,@GroupType,@HierarchyId,@RootId,@DeploymentId,@CorrelationId,@ParentResourceId,@ResourceId,@Name,@Type,@Stage,GETUTCDATE(),GETUTCDATE(),@SubscriptionId,@ManagementGroupId,@Input,@Result,@Comments,@CreateByUserId,@LastRunUserId)
WHEN MATCHED THEN
	UPDATE SET [Stage]=@Stage,[UpdateTimeUtc]=GETUTCDATE(),[Result]=isnull(@Result,[Result]),[Comments]=isnull(@Comments,Comments),[LastRunUserId]=isnull(@LastRunUserId,LastRunUserId),[Input]=isnull(@Input,Input);
", this.options.Database.DeploymentOperationsTableName);
        }

        // TODO: when this is a async task, in orchestration await this will make orchestration cannot be completed, need investigation
        public void SaveDeploymentOperation(DeploymentOperation deploymentOperation)
        {
            TraceActivityEventSource.Log.TraceEvent(
                TraceEventType.Information,
                "DeploymentOperationsActivity",
                deploymentOperation.InstanceId,
                deploymentOperation.ExecutionId,
                $"{deploymentOperation.Type}:{deploymentOperation.ResourceId}:{(int)deploymentOperation.Stage}",
                deploymentOperation.Input,
                deploymentOperation.Stage.ToString());

            using var db = new DbAccess(this.options.Database.ConnectionString);
            db.AddStatement(this._saveDeploymentOperationCommandString, deploymentOperation);
            db.ExecuteNonQueryAsync().Wait();
        }

        public WhatIfOperationResult WhatIf(PredictTemplateOrchestrationInput input)
        {
            var result = new WhatIfOperationResult();
            //var (Result, Message, Deployment) = ParseDeployment(new DeploymentOrchestrationInput()
            //{
            //    CorrelationId = input.CorrelationId,
            //    Parameters = input.Parameters,
            //    ResourceGroup = input.ResourceGroupName,
            //    SubscriptionId = input.SubscriptionId,
            //    TemplateContent = input.Template,
            //    TenantId = input.TenantId
            //});
            //if (!Result)
            //{
            //    result.Status = "failed";
            //    result.Error = new ErrorResponse() { Code = "400", Message = Message };
            //}
            //DeploymentContext deploymentContext = new DeploymentContext()
            //{
            //    CorrelationId = input.CorrelationId,
            //    Mode = input.Mode,
            //    ResourceGroup = input.ResourceGroupName,
            //    SubscriptionId = input.SubscriptionId,
            //    TenantId = input.TenantId,
            //    Parameters = input.Parameters
            //};
            //string queryScope = $"/{infrastructure.BuiltinPathSegment.Subscription}/{input.SubscriptionId}";
            //if (input.ScopeType == ScopeType.ResourceGroup)
            //    queryScope += $"/{infrastructure.BuiltinPathSegment.ResourceGroup}/{input.ResourceGroupName}";
            //var str = this.infrastructure.List(deploymentContext, queryScope, Deployment.Template.ApiProfile, string.Empty, "resources");
            ////https://docs.microsoft.com/en-us/rest/api/resources/resources/listbyresourcegroup#resourcelistresult
            //using var doc = JsonDocument.Parse(str.Content);
            //Dictionary<string, JsonElement> asset = new Dictionary<string, JsonElement>();
            //doc.RootElement.TryGetProperty("values", out JsonElement values);
            //foreach (var r in values.EnumerateArray())
            //{
            //    if (!r.TryGetProperty("id", out JsonElement id))
            //        break;
            //    asset.Add(id.GetString(), r);
            //}

            //foreach (var r in Deployment.Template.Resources.Values)
            //{
            //    CheckResourceWhatIf(input, result, asset, r);
            //}

            //if (input.Mode == DeploymentMode.Complete)
            //{
            //    foreach (var item in asset)
            //    {
            //        result.Changes.Add(new WhatIfChange()
            //        {
            //            ChangeType = ChangeType.Delete,
            //            ResourceId = item.Key
            //        });
            //    }
            //}
            //else
            //{
            //    foreach (var item in asset)
            //    {
            //        result.Changes.Add(new WhatIfChange()
            //        {
            //            ChangeType = ChangeType.Ignore,
            //            ResourceId = item.Key
            //        });
            //    }
            //}

            //result.Status = "succeeded";
            return result;
        }

        private void CheckResourceWhatIf(PredictTemplateOrchestrationInput input, WhatIfOperationResult result, Dictionary<string, JsonElement> asset, Resource resource)
        {
            if (asset.TryGetValue(resource.Name, out JsonElement r))
            {
                if (input.ResultFormat == WhatIfResultFormat.ResourceIdOnly)
                {
                    result.Changes.Add(new WhatIfChange()
                    {
                        ChangeType = ChangeType.Deploy
                    });
                }
                else
                {
                    // TODO: support WhatIfResultFormat.FullResourcePayloads
                    result.Changes.Add(new WhatIfChange()
                    {
                        ChangeType = ChangeType.Modify
                    });
                }

                asset.Remove(resource.Name);
            }
            else
            {
                result.Changes.Add(new WhatIfChange()
                {
                    ChangeType = ChangeType.Create,
                    ResourceId = resource.ResourceId
                });
            }
        }
    
        [Obsolete("using Resource.ExpandProperties(DeploymentContext deploymentContext,ARMFunctions functions,IInfrastructure infrastructure) instead")]
        public string ExpadResourceProperties(Resource resource, DeploymentContext deploymentContext)
        {
            return resource.ExpandProperties(deploymentContext, ARMfunctions, infrastructure);
        }
    }
}