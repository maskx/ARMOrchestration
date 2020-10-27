using DurableTask.Core;
using DurableTask.Core.Exceptions;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestration : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
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
            input.Input.IsRuntime = true;
            if (!context.IsReplaying)
            {
                templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                {
                    InstanceId = context.OrchestrationInstance.InstanceId,
                    ExecutionId = context.OrchestrationInstance.ExecutionId,
                    Stage = ProvisioningStage.StartProvisioning,
                    Input=DataConverter.Serialize(input)
                });
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
                        DependsOn = input.Resource.DependsOn.ToList(),
                        DeploymentId = input.Input.DeploymentId,
                        RootId = input.Input.RootId,
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        DeploymentOperation = new DeploymentOperation(input.Input, input.Resource)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId
                        }
                    });
                }
                catch (TaskFailedException ex)
                {
                    var response = new ErrorResponse()
                    {
                        Code = $"{ResourceOrchestration.Name}:{ProvisioningStage.DependsOnWaited}",
                        Message = ex.Message,
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                    };
                    templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.DependsOnWaitedFailed,
                        Input = DataConverter.Serialize(input),
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
                    templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.DependsOnWaitedFailed,
                        Input = DataConverter.Serialize(input),
                        Result = DataConverter.Serialize(r)
                    });
                    return r;
                }
            }

            #endregion DependsOn

            #region plug-in

            if (infrastructure.InjectBefroeProvisioning)
            {
                try
                {
                    var injectBefroeProvisioningResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                                RequestOrchestration.Name,
                                                "1.0",
                                                new AsyncRequestActivityInput()
                                                {
                                                    InstanceId = context.OrchestrationInstance.InstanceId,
                                                    ExecutionId = context.OrchestrationInstance.ExecutionId,
                                                    ProvisioningStage = ProvisioningStage.InjectBefroeProvisioning
                                                });
                    if (injectBefroeProvisioningResult.Code != 200)
                    {
                        templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.InjectBefroeProvisioningFailed,
                            Input = DataConverter.Serialize(input),
                            Result = DataConverter.Serialize(injectBefroeProvisioningResult)
                        });
                        return injectBefroeProvisioningResult;
                    }
                }
                catch (TaskFailedException ex)
                {
                    var response = new ErrorResponse()
                    {
                        Code = $"{ResourceOrchestration.Name}:{ProvisioningStage.InjectBefroeProvisioning}",
                        Message = ex.Message,
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                    };
                    templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.InjectBefroeProvisioning,
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
                            // doesnot SafeSaveDeploymentOperation, should SafeSaveDeploymentOperation in plugin orchestration
                            return r;
                        }

                        input = r.Content as ResourceOrchestrationInput;
                        input.ServiceProvider = _ServiceProvider;
                    }
                    catch (TaskFailedException ex)
                    {
                        var response = new ErrorResponse()
                        {
                            Code = $"{ResourceOrchestration.Name}:{ProvisioningStage.BeforeResourceProvisioning}",
                            Message = ex.Message,
                            AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                        };
                        templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
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
                                     RequestOrchestration.Name,
                                     "1.0",
                                     new AsyncRequestActivityInput()
                                     {
                                         InstanceId = context.OrchestrationInstance.InstanceId,
                                         ExecutionId = context.OrchestrationInstance.ExecutionId,
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
                    Code = $"{ResourceOrchestration.Name}:{ProvisioningStage.ProvisioningResource}",
                    Message = ex.Message,
                    AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                };
                templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                {
                    InstanceId = context.OrchestrationInstance.InstanceId,
                    ExecutionId = context.OrchestrationInstance.ExecutionId,
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
                            // doesnot SafeSaveDeploymentOperation, should SafeSaveDeploymentOperation in plugin orchestration
                            return r;
                        }
                        input = r.Content as ResourceOrchestrationInput;
                        input.ServiceProvider = _ServiceProvider;
                    }
                    catch (TaskFailedException ex)
                    {
                        var response = DataConverter.Serialize(new ErrorResponse()
                        {
                            Code = $"{ResourceOrchestration.Name}:{ProvisioningStage.AfterResourceProvisioningOrchestation}",
                            Message = ex.Message,
                            AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                        });
                        templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.AfterResourceProvisioningOrchestation,
                            Result = response
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
                             RequestOrchestration.Name,
                             "1.0",
                             new AsyncRequestActivityInput()
                             {
                                 InstanceId = context.OrchestrationInstance.InstanceId,
                                 ExecutionId = context.OrchestrationInstance.ExecutionId,
                                 ProvisioningStage = ProvisioningStage.InjectAfterProvisioning
                             });
                    if (injectAfterProvisioningResult.Code != 200)
                    {

                        return injectAfterProvisioningResult;
                    }
                }
                catch (TaskFailedException ex)
                {
                    var response = new ErrorResponse()
                    {
                        Code = $"{ResourceOrchestration.Name}:{ProvisioningStage.AfterResourceProvisioningOrchestation}",
                        Message = ex.Message,
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                    };
                    templateHelper.SafeSaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.AfterResourceProvisioningOrchestation,
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

            templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = ProvisioningStage.Successed,
                Input = DataConverter.Serialize(input)
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