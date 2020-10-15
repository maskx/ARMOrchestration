﻿using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.SQL;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration
{
    public class ARMTemplateHelper
    {
        private readonly ARMOrchestrationOptions options;
        private readonly string _saveDeploymentOperationCommandString;
     
        public ARMTemplateHelper(
            IOptions<ARMOrchestrationOptions> options)
        {
            this.options = options?.Value;
           
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
	UPDATE SET [Stage]=@Stage,[UpdateTimeUtc]=GETUTCDATE(),[Result]=isnull(cast(@Result AS NVARCHAR(MAX)),[Result]),[Comments]=isnull(@Comments,Comments),[LastRunUserId]=isnull(@LastRunUserId,LastRunUserId),[Input]=isnull(cast(@Input AS NVARCHAR(MAX)),[Input]);
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

            using var db = new SQLServerAccess(this.options.Database.ConnectionString);
            db.AddStatement(this._saveDeploymentOperationCommandString, deploymentOperation);
            db.ExecuteNonQueryAsync().Wait();
        }
        public void SafeSaveDeploymentOperation(DeploymentOperation deploymentOperation)
        {
            try
            {
                SaveDeploymentOperation(deploymentOperation);
            }
            catch
            {

                // Eat up any exception
            }
        }
        public void ProvisioningResource(Resource resource, List<Task<TaskResult>> tasks, OrchestrationContext orchestrationContext, DeploymentOrchestrationInput input)
        {
            tasks.Add(orchestrationContext.CreateSubOrchestrationInstance<TaskResult>(
                                      ResourceOrchestration.Name,
                                      "1.0",
                                      new ResourceOrchestrationInput()
                                      {
                                          Resource = resource,
                                          Input = input,
                                      }));
            foreach (var child in resource.FlatEnumerateChild())
            {
                tasks.Add(orchestrationContext.CreateSubOrchestrationInstance<TaskResult>(
                                                     ResourceOrchestration.Name,
                                                     "1.0",
                                                     new ResourceOrchestrationInput()
                                                     {
                                                         Resource = child,
                                                         Input = input,
                                                     }));
            }
        }
        public void ParseTaskResult(string orchestrationName, List<ErrorResponse> errorResponses, Task<TaskResult> item)
        {
            if (item.IsFaulted)
            {
                errorResponses.Add(new ErrorResponse()
                {
                    Code = $"{orchestrationName}-{ProvisioningStage.ProvisioningResourceFailed}",
                    Message = $"Provisioning resource failed in {orchestrationName}",
                    AdditionalInfo = new ErrorAdditionalInfo[] { new ErrorAdditionalInfo() {
                                        Type=item.Exception.GetType().FullName,
                                        Info=item.Exception
                                    } }
                });
            }
            else if (item.IsCanceled)
            {
                errorResponses.Add(new ErrorResponse()
                {
                    Code = $"{orchestrationName}-{ProvisioningStage.ProvisioningResourceFailed}",
                    Message = $"Provisioning resource be canceled in {orchestrationName}"
                });
            }
            else if (item.Result.Code != 200)
            {
                errorResponses.Add(item.Result.Content as ErrorResponse);
            }
        }
        
    }
}