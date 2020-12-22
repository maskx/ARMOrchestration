using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration
{
    public class ARMTemplateHelper
    {
        private readonly ARMOrchestrationOptions options;
        private readonly string _saveDeploymentOperationCommandString;
        private readonly IServiceProvider _ServiceProvider;
        private readonly ARMFunctions _ARMFunctions;
        private readonly DataConverter _DataConverter = new JsonDataConverter();
        public ARMTemplateHelper(
            IOptions<ARMOrchestrationOptions> options,
            ARMFunctions aRMFunctions,
            IServiceProvider serviceProvider)
        {
            this._ServiceProvider = serviceProvider;
            this.options = options?.Value;
            this._ARMFunctions = aRMFunctions;
            this._saveDeploymentOperationCommandString = string.Format(@"
MERGE {0} with (serializable) [Target]
USING (VALUES (@InstanceId,@DeploymentId)) as [Source](InstanceId,DeploymentId)
ON [Target].InstanceId = [Source].InstanceId AND [Target].DeploymentId = [Source].DeploymentId
WHEN NOT MATCHED THEN
	INSERT
	([InstanceId],[ExecutionId],[GroupId],[GroupType],[HierarchyId],[RootId],[DeploymentId],[CorrelationId],[ParentResourceId],[ResourceId],[Name],[Type],[Stage],[CreateTimeUtc],[UpdateTimeUtc],[SubscriptionId],[ManagementGroupId],[Input],[Result],[Comments],[CreateByUserId],[LastRunUserId])
	VALUES
	(@InstanceId,@ExecutionId,@GroupId,@GroupType,@HierarchyId,@RootId,@DeploymentId,@CorrelationId,@ParentResourceId,@ResourceId,@Name,@Type,@Stage,GETUTCDATE(),GETUTCDATE(),@SubscriptionId,@ManagementGroupId,cast(@Input AS NVARCHAR(MAX)),@Result,@Comments,@CreateByUserId,@LastRunUserId)
WHEN MATCHED THEN
	UPDATE SET [ExecutionId]=isnull(@ExecutionId,ExecutionId),[Stage]=@Stage,[UpdateTimeUtc]=GETUTCDATE(),[Result]=isnull(cast(@Result AS NVARCHAR(MAX)),[Result]),[Comments]=isnull(@Comments,Comments),[LastRunUserId]=isnull(@LastRunUserId,LastRunUserId),[Input]=isnull(cast(@Input AS NVARCHAR(MAX)),[Input]);
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
        public void ProvisioningResource<T>(Resource resource, List<Task<TaskResult>> tasks, OrchestrationContext orchestrationContext)
            where T : CommunicationJob, new()
        {
            tasks.Add(orchestrationContext.CreateSubOrchestrationInstance<TaskResult>(
                                      ResourceOrchestration<T>.Name,
                                      "1.0",
                                      new ResourceOrchestrationInput()
                                      {
                                          DeploymentResourceId = resource.Input.ResourceId,
                                          NameWithServiceType = resource.NameWithServiceType,
                                          ServiceProvider = resource.ServiceProvider,
                                          CopyIndex = resource.CopyIndex ?? -1
                                      }));
            foreach (var child in resource.FlatEnumerateChild())
            {
                tasks.Add(orchestrationContext.CreateSubOrchestrationInstance<TaskResult>(
                                                     ResourceOrchestration<T>.Name,
                                                     "1.0",
                                                     new ResourceOrchestrationInput()
                                                     {
                                                         DeploymentResourceId = child.Input.ResourceId,
                                                         NameWithServiceType = child.NameWithServiceType,
                                                         ServiceProvider = child.ServiceProvider,
                                                         CopyIndex = child.CopyIndex ?? -1
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
                if (item.Result.Content is List<ErrorResponse> errs)
                    errorResponses.AddRange(errs);
                else if (item.Result.Content is ErrorResponse err)
                    errorResponses.Add(err);
                else
                    errorResponses.Add(new ErrorResponse()
                    {
                        Code = item.Result.Code.ToString(),
                        Message = item.Result.Content.ToString()
                    });
            }
        }
        public TemplateLink ParseTemplateLink(JsonElement element, Dictionary<string, object> cxt)
        {
            var templateLink = new TemplateLink();
            if (element.TryGetProperty("contentVersion", out JsonElement cv))
                templateLink.ContentVersion = cv.GetString();
            if (element.TryGetProperty("uri", out JsonElement uri))
                templateLink.Uri = this._ARMFunctions.Evaluate(uri.GetString(), cxt).ToString();
            return templateLink;
        }
        public Deployment GetDeploymentByResourceId(string resouceId)
        {
            Deployment input = null;
            using (var db = new SQLServerAccess(this.options.Database.ConnectionString))
            {
                db.AddStatement($"select Input from {this.options.Database.DeploymentOperationsTableName} where ResourceId=@ResourceId", new { ResourceId = resouceId });
                db.ExecuteReaderAsync((reader, index) =>
                {
                    input = _DataConverter.Deserialize<Deployment>(reader.GetString(0));
                    input.ServiceProvider = this._ServiceProvider;
                }).Wait();
            }
            return input;
        }
        public Deployment GetDeploymentById(string deploymentId,string instanceId)
        {
            Deployment input = null;
            using (var db = new SQLServerAccess(this.options.Database.ConnectionString))
            {
                db.AddStatement($"select Input from {this.options.Database.DeploymentOperationsTableName} where DeploymentId=@DeploymentId and InstanceId=@InstanceId",
                    new
                    {
                        DeploymentId= instanceId,
                        InstanceId= deploymentId
                    });
                db.ExecuteReaderAsync((reader, index) =>
                {
                    input = _DataConverter.Deserialize<Deployment>(reader.GetString(0));
                    input.ServiceProvider = this._ServiceProvider;
                }).Wait();
            }
            return input;
        }
    }
}