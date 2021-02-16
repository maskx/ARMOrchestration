using DurableTask.Core;
using DurableTask.Core.Exceptions;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestration<T> : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
        where T : CommunicationJob, new()
    {
        public const string Name = "ResourceOrchestration";
        private readonly IInfrastructure infrastructure;
        private readonly IServiceProvider _ServiceProvider;
        private readonly ARMTemplateHelper templateHelper;

        public ResourceOrchestration(
            IInfrastructure infrastructure,
            ARMTemplateHelper templateHelper,
            IServiceProvider serviceProvider)
        {
            this._ServiceProvider = serviceProvider;
            this.infrastructure = infrastructure;
            this.templateHelper = templateHelper;
        }
        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            input.ServiceProvider = _ServiceProvider;
            if (!input.IsRetry)
                input.DeploymentOperationId = context.OrchestrationInstance.InstanceId;
            if (!context.IsReplaying)
            {
                if (input.IsRetry)
                {
                    var r = templateHelper.PrepareRetry(input.DeploymentOperationId, context.OrchestrationInstance.InstanceId, context.OrchestrationInstance.ExecutionId, input.LastRunUserId, DataConverter.Serialize(input));

                    if (r == null)
                        return new TaskResult(400, new ErrorResponse()
                        {
                            Code = $"{Name}:PrepareRetry",
                            Message = $"cannot find DeploymentOperation with Id:{input.DeploymentOperationId}"
                        });
                    if (r.Value == ProvisioningStage.Successed)
                        return new TaskResult(200, "");
                    if (r.Value != ProvisioningStage.StartProvisioning)
                        return new TaskResult(400, new ErrorResponse()
                        {
                            Code = $"{Name}:PrepareRetry",
                            Message = $"Deployment[{input.DeploymentOperationId}] in stage of [{r.Value}], only failed deployment support retry"
                        });
                }
                else
                {
                    var operation = templateHelper.CreatDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId, input.Resource)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.StartProvisioning,
                        Input = DataConverter.Serialize(input),
                        LastRunUserId = input.LastRunUserId
                    }).Result;
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
            }

            #region DependsOn

            if (input.Resource.DependsOn.Count > 0)
            {
                dependsOnWaitHandler = new TaskCompletionSource<string>();
                try
                {
                    await context.ScheduleTask<TaskResult>(WaitDependsOnActivity.Name, "1.0",
                    new WaitDependsOnActivityInput()
                    {
                        DeploymentOperationId = input.DeploymentOperationId,
                        DependsOn = input.Resource.DependsOn.ToList(),
                        DeploymentId = input.Deployment.DeploymentId,
                        RootId = input.Deployment.RootId
                    });
                }
                catch (TaskFailedException ex)
                {
                    var response = new ErrorResponse()
                    {
                        Code = $"{ResourceOrchestration<T>.Name}:{ProvisioningStage.DependsOnWaited}",
                        Message = ex.Message,
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                    };
                    templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
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

                await dependsOnWaitHandler.Task;
                var r = DataConverter.Deserialize<TaskResult>(dependsOnWaitHandler.Task.Result);
                if (r.Code != 200)
                {
                    templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                    {
                        Stage = ProvisioningStage.DependsOnWaitedFailed,
                        Result = DataConverter.Serialize(r)
                    });
                    return r;
                }
            }

            #endregion DependsOn

            input.Deployment.IsRuntime = true;

            #region plug-in

            if (infrastructure.InjectBefroeProvisioning)
            {
                try
                {
                    var injectBefroeProvisioningResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                                RequestOrchestration<T>.Name,
                                                "1.0",
                                                new AsyncRequestActivityInput()
                                                {
                                                    DeploymentOperationId = input.DeploymentOperationId,
                                                    ProvisioningStage = ProvisioningStage.InjectBefroeProvisioning
                                                });
                    if (injectBefroeProvisioningResult.Code != 200)
                    {
                        templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                        {
                            Stage = ProvisioningStage.InjectBefroeProvisioningFailed,
                            Result = DataConverter.Serialize(injectBefroeProvisioningResult)
                        });
                        return injectBefroeProvisioningResult;
                    }
                }
                catch (TaskFailedException ex)
                {
                    var response = new ErrorResponse()
                    {
                        Code = $"{ResourceOrchestration<T>.Name}:{ProvisioningStage.InjectBefroeProvisioning}",
                        Message = ex.Message,
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                    };
                    templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                    {
                        Stage = ProvisioningStage.InjectBefroeProvisioningFailed,
                        Result = DataConverter.Serialize(response)
                    });
                    return new TaskResult()
                    {
                        Code = 500,
                        Content = response
                    };
                }
            }

            if (infrastructure.BeforeResourceProvisioningOrchestation != null)
            {
                foreach (var t in infrastructure.BeforeResourceProvisioningOrchestation)
                {
                    try
                    {
                        var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                        if (r.Code != 200)
                        {
                            templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                            {
                                Stage = ProvisioningStage.BeforeResourceProvisioningFailed,
                                Result = DataConverter.Serialize(r)
                            });
                            return r;
                        }
                    }
                    catch (TaskFailedException ex)
                    {
                        var response = new ErrorResponse()
                        {
                            Code = $"{ResourceOrchestration<T>.Name}:{ProvisioningStage.BeforeResourceProvisioning}",
                            Message = ex.Message,
                            AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                        };
                        templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                        {
                            Stage = ProvisioningStage.BeforeResourceProvisioning,
                            Result = DataConverter.Serialize(response)
                        });
                        return new TaskResult()
                        {
                            Code = 500,
                            Content = response
                        };
                    }
                }
            }

            #endregion plug-in

            #region Provisioning Resource

            try
            {
                var createResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                     RequestOrchestration<T>.Name,
                                     "1.0",
                                     new AsyncRequestActivityInput()
                                     {
                                         DeploymentOperationId = input.DeploymentOperationId,
                                         ProvisioningStage = ProvisioningStage.ProvisioningResource
                                     });
                if (createResourceResult.Code != 200)
                {
                    return createResourceResult;
                }
            }
            catch (TaskFailedException ex)
            {
                var response = new ErrorResponse()
                {
                    Code = $"{ResourceOrchestration<T>.Name}:{ProvisioningStage.ProvisioningResource}",
                    Message = ex.Message,
                    AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                };
                templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                {
                    Stage = ProvisioningStage.ProvisioningResource,
                    Result = DataConverter.Serialize(response)
                });
                return new TaskResult()
                {
                    Code = 500,
                    Content = response
                };
            }


            #endregion Provisioning Resource

            #region After Resource Provisioning

            if (infrastructure.AfterResourceProvisioningOrchestation != null)
            {
                foreach (var t in infrastructure.AfterResourceProvisioningOrchestation)
                {
                    try
                    {
                        var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                        if (r.Code != 200)
                        {
                            templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                            {
                                Stage = ProvisioningStage.AfterResourceProvisioningOrchestationFailed,
                                Result = DataConverter.Serialize(r)
                            });
                            return r;
                        }
                    }
                    catch (TaskFailedException ex)
                    {
                        var response = new ErrorResponse()
                        {
                            Code = $"{ResourceOrchestration<T>.Name}:{ProvisioningStage.AfterResourceProvisioningOrchestation}",
                            Message = ex.Message,
                            AdditionalInfo = new ErrorAdditionalInfo[] {
                                new ErrorAdditionalInfo() {
                                    Type=typeof(TaskFailedException).FullName,
                                    Info=ex
                                }
                            }
                        };
                        templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                        {
                            Stage = ProvisioningStage.AfterResourceProvisioningOrchestationFailed,
                            Result = DataConverter.Serialize(response)
                        });
                        return new TaskResult()
                        {
                            Code = 500,
                            Content = response
                        };
                    }
                }
            }

            #endregion After Resource Provisioning

            if (infrastructure.InjectAfterProvisioning)
            {
                try
                {
                    var injectAfterProvisioningResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                             RequestOrchestration<T>.Name,
                             "1.0",
                             new AsyncRequestActivityInput()
                             {
                                 DeploymentOperationId = input.DeploymentOperationId,
                                 ProvisioningStage = ProvisioningStage.InjectAfterProvisioning
                             });
                    if (injectAfterProvisioningResult.Code != 200)
                    {
                        templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId, input.Resource)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.InjectAfterProvisioningFailed,
                            Result = DataConverter.Serialize(injectAfterProvisioningResult)
                        });
                        return injectAfterProvisioningResult;
                    }
                }
                catch (TaskFailedException ex)
                {
                    var response = new ErrorResponse()
                    {
                        Code = $"{ResourceOrchestration<T>.Name}:{ProvisioningStage.InjectAfterProvisioning}",
                        Message = ex.Message,
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                    };
                    templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                    {
                        Stage = ProvisioningStage.InjectAfterProvisioningFailed,
                        Result = DataConverter.Serialize(response)
                    });
                    return new TaskResult()
                    {
                        Code = 500,
                        Content = response
                    };
                }

            }

            #region Ready Resource

            templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
            {
                Stage = ProvisioningStage.Successed
            });

            #endregion Ready Resource

            return new TaskResult() { Code = 200 };
        }

        private TaskCompletionSource<string> dependsOnWaitHandler = null;

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.dependsOnWaitHandler != null && name == ProvisioningStage.DependsOnWaited.ToString() && this.dependsOnWaitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.dependsOnWaitHandler.SetResult(input);
            }
        }
    }
}