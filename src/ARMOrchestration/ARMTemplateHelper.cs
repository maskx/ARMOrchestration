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
        private readonly IInfrastructure _Infrastructure;
        public ARMTemplateHelper(
            IOptions<ARMOrchestrationOptions> options,
            ARMFunctions aRMFunctions,
            IServiceProvider serviceProvider,
            IInfrastructure infrastructure)
        {
            this._Infrastructure = infrastructure;
            this._ServiceProvider = serviceProvider;
            this.options = options?.Value;
            this._ARMFunctions = aRMFunctions;
            this._saveDeploymentOperationCommandString = string.Format(
                @"UPDATE {0} 
SET [InstanceId]=isnull(@InstanceId,InstanceId),[ExecutionId]=isnull(@ExecutionId,ExecutionId),[Stage]=@Stage,[UpdateTimeUtc]=GETUTCDATE(),[Result]=isnull(cast(@Result AS NVARCHAR(MAX)),[Result]),[Comments]=isnull(@Comments,Comments),[LastRunUserId]=isnull(@LastRunUserId,LastRunUserId),[Input]=isnull(cast(@Input AS NVARCHAR(MAX)),[Input])
where Id=@Id
;"
                , this.options.Database.DeploymentOperationsTableName);
        }
        public async Task<DeploymentOperation> CreateDeploymentOperation(DeploymentOperation deploymentOperation)
        {
            DeploymentOperation rtv = null;
            using var db = new SQLServerAccess(options.Database.ConnectionString);
            await db.ExecuteStoredProcedureASync(options.Database.CreateDeploymentOperationSPName,
                (reader, index) =>
                {
                    rtv = new DeploymentOperation(reader["Id"].ToString())
                    {
                        InstanceId = reader["InstanceId"].ToString(),
                        ExecutionId = reader["ExecutionId"]?.ToString(),
                        GroupId = reader["GroupId"].ToString(),
                        GroupType = reader["GroupType"].ToString(),
                        HierarchyId = reader["HierarchyId"].ToString(),
                        RootId = reader["RootId"].ToString(),
                        DeploymentId = reader["DeploymentId"].ToString(),
                        CorrelationId = reader["CorrelationId"].ToString(),
                        ResourceId = reader["ResourceId"].ToString(),
                        Name = reader["Name"].ToString(),
                        Type = reader["Type"].ToString(),
                        Stage = (ProvisioningStage)(int)reader["Stage"],
                        SubscriptionId = reader["SubscriptionId"]?.ToString(),
                        ManagementGroupId = reader["ManagementGroupId"]?.ToString(),
                        ParentResourceId = reader["ParentResourceId"]?.ToString(),
                        Input = reader["Input"]?.ToString(),
                        Result = reader["Result"]?.ToString(),
                        CreateByUserId = reader["CreateByUserId"].ToString(),
                        LastRunUserId = reader["LastRunUserId"].ToString(),
                        CreateTimeUtc = (DateTime)reader["CreateTimeUtc"],
                        UpdateTimeUtc = (DateTime)reader["UpdateTimeUtc"],
                        ApiVersion = reader["ApiVersion"].ToString(),
                        Comments = reader["Comments"]?.ToString()
                    };
                },
                new
                {
                    deploymentOperation.Id,
                    deploymentOperation.DeploymentId,
                    deploymentOperation.InstanceId,
                    deploymentOperation.RootId,
                    deploymentOperation.CorrelationId,
                    deploymentOperation.GroupId,
                    deploymentOperation.GroupType,
                    deploymentOperation.HierarchyId,
                    deploymentOperation.ResourceId,
                    deploymentOperation.Name,
                    deploymentOperation.Type,
                    deploymentOperation.CreateByUserId,
                    deploymentOperation.ApiVersion,
                    deploymentOperation.ExecutionId,
                    deploymentOperation.Comments,
                    deploymentOperation.SubscriptionId,
                    deploymentOperation.ManagementGroupId,
                    deploymentOperation.ParentResourceId,
                    deploymentOperation.Input,
                    deploymentOperation.Stage
                });
            return rtv;
        }

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
            if (db.ExecuteNonQueryAsync().Result != 1)
            {
                throw new Exception($"cannot find DeploymentOperation with Id[{deploymentOperation.Id}]");
            }
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
        public void ProvisioningResource<T>(Resource resource, List<Task<TaskResult>> tasks, OrchestrationContext orchestrationContext, bool isRetry, string lastRunUserId)
            where T : CommunicationJob, new()
        {
            if (!resource.Condition)
                return;
            // copy should be executed before BuiltinServiceTypes.Deployments
            // because BuiltinServiceTypes.Deployments can be a copy resource
            if (resource.Copy != null && !resource.CopyIndex.HasValue)
            {
                tasks.Add(orchestrationContext.CreateSubOrchestrationInstance<TaskResult>(
                CopyOrchestration<T>.Name,
                "1.0",
                new ResourceOrchestrationInput()
                {
                    DeploymentId = resource.Deployment.DeploymentId,
                    ResourceId = resource.Copy.Id,
                    IsRetry = isRetry,
                    CopyIndex = -1,
                    LastRunUserId = lastRunUserId
                }));
            }
            else if (resource.Type == _Infrastructure.BuiltinServiceTypes.Deployments)
            {
                tasks.Add(orchestrationContext.CreateSubOrchestrationInstance<TaskResult>(
                    DeploymentOrchestration<T>.Name,
                    "1.0",
                    _DataConverter.Serialize(new ResourceOrchestrationInput()
                    {
                        DeploymentId = resource.Deployment.DeploymentId,
                        ResourceId = resource.CopyIndex.HasValue ? resource.Copy.Id : resource.ResourceId,
                        CopyIndex = resource.CopyIndex ?? -1,
                        IsRetry = isRetry,
                        LastRunUserId = lastRunUserId
                    })));
            }
            else
            {
                tasks.Add(orchestrationContext.CreateSubOrchestrationInstance<TaskResult>(
                                     ResourceOrchestration<T>.Name,
                                     "1.0",
                                     new ResourceOrchestrationInput()
                                     {
                                         DeploymentId = resource.Deployment.DeploymentId,
                                         ResourceId = resource.CopyIndex.HasValue ? resource.Copy.Id : resource.ResourceId,
                                         CopyIndex = resource.CopyIndex ?? -1,
                                         IsRetry = isRetry,
                                         LastRunUserId = lastRunUserId
                                     }));
            }
            foreach (var child in resource.FlatEnumerateChild())
            {
                ProvisioningResource<T>(child, tasks, orchestrationContext, isRetry, lastRunUserId);
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
        public async Task<T> GetInputAsync<T>(string deploymentOperationId)
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
        public async Task<Deployment> GetDeploymentAsync(string deploymentId)
        {
            Deployment input = null;
            using (var db = new SQLServerAccess(this.options.Database.ConnectionString))
            {
                db.AddStatement($"select Input from {this.options.Database.DeploymentOperationsTableName} where DeploymentId=@DeploymentId and [Type]=N'{_Infrastructure.BuiltinServiceTypes.Deployments}'",
                    new { DeploymentId = deploymentId });
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    input = _DataConverter.Deserialize<Deployment>(reader.GetString(0));
                    input.ServiceProvider = _ServiceProvider;
                });
            }
            return input;
        }
        public async Task<ProvisioningStage> GetProvisioningStageAsync(string deploymentOperationId)
        {
            ProvisioningStage stage = ProvisioningStage.Pending;
            using (var db = new SQLServerAccess(options.Database.ConnectionString))
            {
                db.AddStatement($"select Stage from {options.Database.DeploymentOperationsTableName} where Id=@Id", new { Id = deploymentOperationId });
                await db.ExecuteReaderAsync((reader, index) =>
                {
                    stage = (ProvisioningStage)reader.GetInt32(0);
                });
            }
            return stage;
        }
        public ProvisioningStage? PrepareRetry(string id, string newInstanceId, string newExecutionId, string lastRunUserId, string input = null)
        {
            using var db = new SQLServerAccess(options.Database.ConnectionString);
            var r = db.ExecuteScalarAsync(options.Database.RetrySPName, new
            {
                Id = id,
                NewInstanceId = newInstanceId,
                NewExecutionId = newExecutionId,
                LastRunUserId = lastRunUserId,
                Input = input
            }).Result;
            if (r == null || r == DBNull.Value)
                return null;
            return (ProvisioningStage)(int)r;
        }
        public async Task<List<string>> GetAllRetryResource(string deploymentOperationId, bool deepSearch = true)
        {
            List<string> rtv = new List<string>();
            using var db = new SQLServerAccess(this.options.Database.ConnectionString);
            db.AddStatement(@$"
select op.Id,op.ResourceId from {this.options.Database.DeploymentOperationsTableName} as op
inner join {this.options.Database.DeploymentOperationsTableName} as dp on dp.ResourceId=op.ParentResourceId
where dp.Id=@Id and op.Stage={(int)ProvisioningStage.Pending}",
new
{
    Id = deploymentOperationId
});
            await db.ExecuteReaderAsync((reader, index) =>
            {
                rtv.Add(reader.GetString(1));
                if (deepSearch)
                    rtv.AddRange(GetAllRetryResource(reader.GetString(0)).Result);

            });
            return rtv;
        }
        public (ProvisioningStage? Stage, string DeploymentOpeartionId) PrepareRetry(ResourceOrchestrationInput input, string newInstanceId, string newExecutionId)
        {
            ProvisioningStage? Stage = null;
            string DeploymentOpeartionId = null;
            using var db = new SQLServerAccess(options.Database.ConnectionString);
            db.ExecuteStoredProcedureASync(options.Database.RetryResourceSPName,
                (reader, index) =>
                {
                    DeploymentOpeartionId = reader.GetString(0);
                    Stage = (ProvisioningStage)reader.GetInt32(1);
                },
                new
                {
                    input.DeploymentId,
                    input.ResourceId,
                    NewInstanceId = newInstanceId,
                    NewExecutionId = newExecutionId,
                    input.LastRunUserId,
                    Input = _DataConverter.Serialize(input)
                }).Wait();
            return (Stage, DeploymentOpeartionId);
        }
    }
}