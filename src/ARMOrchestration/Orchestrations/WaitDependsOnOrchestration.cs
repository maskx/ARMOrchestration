using DurableTask.Core;
using DurableTask.Core.Serializing;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using maskx.OrchestrationService.SQL;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class WaitDependsOnOrchestration<T> : TaskOrchestration<TaskResult, WaitDependsOnOrchestrationInput>
         where T : CommunicationJob, new()
    {
        private TaskCompletionSource<string> dependsOnWaitHandler = null;
        private readonly ARMTemplateHelper _Helper;
        private readonly IServiceProvider _ServiceProvider;
        public const string Name = "WaitDependsOnOrchestration";
        public WaitDependsOnOrchestration(ARMTemplateHelper helper, IServiceProvider serviceProvider)
        {
            _Helper = helper;
            _ServiceProvider = serviceProvider;
        }
        public override async Task<TaskResult> RunTask(OrchestrationContext context, WaitDependsOnOrchestrationInput input)
        {
            dependsOnWaitHandler = new TaskCompletionSource<string>();
            input.ServiceProvider = _ServiceProvider;
            if (!context.IsReplaying)
            {
                try
                {
                    if (input.IsRetry)
                    {
                        _Helper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                        {
                            Stage = ProvisioningStage.DependsOnWaited
                        });
                    }
                    else
                    {
                        DeploymentOperation operation = null;
                        if (string.IsNullOrEmpty(input.ResourceId))
                        {
                            operation = await _Helper.CreatDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId, input.Deployment)
                            {
                                InstanceId = input.InstanceId,
                                ExecutionId = input.ExecutionId,
                                Stage = ProvisioningStage.DependsOnWaited,
                                LastRunUserId = input.LastRunUserId
                            });
                        }
                        else
                        {
                            operation = await _Helper.CreatDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId, input.Resource)
                            {
                                InstanceId = input.InstanceId,
                                ExecutionId = input.ExecutionId,
                                Stage = ProvisioningStage.DependsOnWaited,
                                LastRunUserId = input.LastRunUserId
                            });
                        }
                        if (operation == null)
                            return new TaskResult(400, new ErrorResponse()
                            {
                                Code = $"{Name}:CreatDeploymentOperation",
                                Message = "CorrelationId duplicated"
                            });
                        if (operation.Id != input.DeploymentOperationId)
                            return new TaskResult(400, new ErrorResponse()
                            {
                                Code = $"{Name}:CreatDeploymentOperation",
                                Message = $"{operation.ResourceId} already exists"
                            });
                    }
                    ARMOrchestrationOptions options = _ServiceProvider.GetService<IOptions<ARMOrchestrationOptions>>().Value;
                    using (var db = new SQLServerAccess(options.Database.ConnectionString, _ServiceProvider.GetService<ILoggerFactory>()))
                    {
                        var dependsOn = string.IsNullOrEmpty(input.ResourceId) ? input.Deployment.DependsOn : input.Resource.DependsOn;
                        foreach (var item in dependsOn)
                        {
                            db.AddStatement(string.Format(@"insert into {0}
(RootId,DeploymentId,InstanceId,ExecutionId,EventName,DependsOnName,CreateTime)
values
(@RootId,@DeploymentId,@InstanceId,@ExecutionId,@EventName,@DependsOnName,GETUTCDATE())", options.Database.WaitDependsOnTableName),
new
{
    input.Deployment.RootId,
    input.DeploymentId,
    context.OrchestrationInstance.InstanceId,
    context.OrchestrationInstance.ExecutionId,
    EventName = ProvisioningStage.DependsOnWaited.ToString(),
    DependsOnName = item
});
                        }
                        await db.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception ex)
                {
                    var response = new ErrorResponse()
                    {
                        Code = $"{WaitDependsOnOrchestration<T>.Name}:{ProvisioningStage.DependsOnWaited}",
                        Message = ex.Message,
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(Exception).FullName,
                            Info=ex
                        } }
                    };
                    _Helper.SafeSaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                    {
                        Stage = ProvisioningStage.DependsOnWaitedFailed,
                        Result = DataConverter.Serialize(response)
                    });
                    return new TaskResult()
                    {
                        Code = 500,
                        Content = response
                    };
                }

            }

            await dependsOnWaitHandler.Task;
            var r = DataConverter.Deserialize<TaskResult>(dependsOnWaitHandler.Task.Result);
            if (r.Code != 200)
            {
                _Helper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                {
                    Stage = ProvisioningStage.DependsOnWaitedFailed,
                    Result = DataConverter.Serialize(r)
                });
                return r;
            }
            input.Deployment.IsRuntime = true;
            if (string.IsNullOrEmpty(input.ResourceId))
            {
                _Helper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                {
                    Result = DataConverter.Serialize(r),
                    Input = DataConverter.Serialize(input.Deployment)
                });
            }
            else
            {
                input.Resource.Refresh();
                _Helper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                {
                    Result = DataConverter.Serialize(r),
                    Input = DataConverter.Serialize(input.Resource)
                });
            }

            return new TaskResult(200, "");
        }
        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.dependsOnWaitHandler != null && name == ProvisioningStage.DependsOnWaited.ToString() && this.dependsOnWaitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.dependsOnWaitHandler.SetResult(input);
            }
        }
    }
}
