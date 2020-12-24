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
USING (VALUES (@Id)) as [Source](Id)
ON [Target].Id = [Source].Id
WHEN NOT MATCHED THEN
	INSERT
	([Id],[InstanceId],[ExecutionId],[GroupId],[GroupType],[HierarchyId],[RootId],[DeploymentId],[CorrelationId],[ParentResourceId],[ResourceId],[Name],[Type],[Stage],[CreateTimeUtc],[UpdateTimeUtc],[SubscriptionId],[ManagementGroupId],[Input],[Result],[Comments],[CreateByUserId],[LastRunUserId])
	VALUES
	(@Id,@InstanceId,@ExecutionId,@GroupId,@GroupType,@HierarchyId,@RootId,@DeploymentId,@CorrelationId,@ParentResourceId,@ResourceId,@Name,@Type,@Stage,GETUTCDATE(),GETUTCDATE(),@SubscriptionId,@ManagementGroupId,cast(@Input AS NVARCHAR(MAX)),@Result,@Comments,@CreateByUserId,@LastRunUserId)
WHEN MATCHED THEN
	UPDATE SET [InstanceId]=isnull(@InstanceId,InstanceId),[ExecutionId]=isnull(@ExecutionId,ExecutionId),[Stage]=@Stage,[UpdateTimeUtc]=GETUTCDATE(),[Result]=isnull(cast(@Result AS NVARCHAR(MAX)),[Result]),[Comments]=isnull(@Comments,Comments),[LastRunUserId]=isnull(@LastRunUserId,LastRunUserId),[Input]=isnull(cast(@Input AS NVARCHAR(MAX)),[Input]);
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
        public void ProvisioningResource<T>(Resource resource, List<Task<TaskResult>> tasks, OrchestrationContext orchestrationContext, bool isRetry,string lastRunUserId)
            where T : CommunicationJob, new()
        {
            tasks.Add(orchestrationContext.CreateSubOrchestrationInstance<TaskResult>(
                                      ResourceOrchestration<T>.Name,
                                      "1.0",
                                      new ResourceOrchestrationInput()
                                      {
                                          DeploymentOperationId=resource.DeploymentOperationId,
                                          DeploymentId = resource.Input.DeploymentId,
                                          NameWithServiceType = resource.NameWithServiceType,
                                          ServiceProvider = resource.ServiceProvider,
                                          CopyIndex = resource.CopyIndex ?? -1,
                                          IsRetry = isRetry,
                                          LastRunUserId= lastRunUserId
                                      }));
            foreach (var child in resource.FlatEnumerateChild())
            {
                tasks.Add(orchestrationContext.CreateSubOrchestrationInstance<TaskResult>(
                                                     ResourceOrchestration<T>.Name,
                                                     "1.0",
                                                     new ResourceOrchestrationInput()
                                                     {
                                                         DeploymentOperationId=child.DeploymentOperationId,
                                                         DeploymentId = child.Input.DeploymentId,
                                                         NameWithServiceType = child.NameWithServiceType,
                                                         ServiceProvider = child.ServiceProvider,
                                                         CopyIndex = child.CopyIndex ?? -1,
                                                         IsRetry = isRetry,
                                                         LastRunUserId=lastRunUserId
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
        public async Task<T> GetInputByDeploymentOperationId<T>(string deploymentOperationId)
        {
            T input = default;
            using (var db = new SQLServerAccess(this.options.Database.ConnectionString))
            {
                db.AddStatement($"select Input from {this.options.Database.DeploymentOperationsTableName} where Id=@Id",
                    new { Id = deploymentOperationId });
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    input = _DataConverter.Deserialize<T>(reader.GetString(0));
                });
            }
            return input;
        }
        public async Task<ResourceOrchestrationInput> GetResourceOrchestrationInput(string deploymentOperationId)
        {
            ResourceOrchestrationInput input = await GetInputByDeploymentOperationId<ResourceOrchestrationInput>(deploymentOperationId);
            input.ServiceProvider = this._ServiceProvider;
            return input;
        }
        public async Task<Deployment> GetDeploymentById(string deploymentId)
        {
            // deploymentId === deploymentOperationId when type=deployment
            var deployment = await GetInputByDeploymentOperationId<Deployment>(deploymentId);
            deployment.ServiceProvider = _ServiceProvider;
            return deployment;
        }
        public async Task<ProvisioningStage> GetProvisioningStage(string deploymentOperationId)
        {
            ProvisioningStage stage = ProvisioningStage.Pending;
            using (var db = new SQLServerAccess(options.Database.ConnectionString))
            {
                db.AddStatement($"select Stage from {options.Database.DeploymentOperationsTableName} where Id=@Id", new { Id = deploymentOperationId });
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    stage = (ProvisioningStage)(int)reader["Stage"];
                });
            }
            return stage;
        }
        public bool PrepareRetry(string deploymentId, string instanceId, string newInstanceId, string userId, string input = null)
        {
            using var db = new SQLServerAccess(options.Database.ConnectionString);
            db.AddStatement(@$"update {options.Database.DeploymentOperationsTableName}
set InstanceId=@NewInstanceId,Stage=@Stage,LastRunUserId=@UserId,Input=isnull(@Input,Input)
where InstanceId=@InstanceId and DeploymentId=@DeploymentId",
new
{
InstanceId = instanceId,
NewInstanceId = newInstanceId,
DeploymentId = deploymentId,
Input = input,
UserId = userId,
Stage = ProvisioningStage.StartProvisioning
});
            if (1 != db.ExecuteNonQueryAsync().Result)
                return false;
            return true;
        }
    }
}