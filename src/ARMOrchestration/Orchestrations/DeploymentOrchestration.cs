using DurableTask.Core;
using DurableTask.Core.Exceptions;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestration<T> : TaskOrchestration<TaskResult, string>
        where T : CommunicationJob, new()
    {
        public const string Name = "DeploymentOrchestration";
        private readonly ARMTemplateHelper helper;
        private readonly IInfrastructure infrastructure;
        private readonly IServiceProvider _ServiceProvider;

        public DeploymentOrchestration(
            ARMTemplateHelper helper,
            IInfrastructure infrastructure,
            IServiceProvider serviceProvider)
        {
            this._ServiceProvider = serviceProvider;
            this.helper = helper;
            this.infrastructure = infrastructure;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, string arg)
        {
            var input = this.DataConverter.Deserialize<Deployment>(arg);
            input.ServiceProvider = this._ServiceProvider;
            input.IsRuntime = true;
            if (!context.IsReplaying)
            {
                // for persistence variable, cos function like newGuid() should always return same value in variable
                var _ = input.Template.Variables;
                helper.SaveDeploymentOperation(new DeploymentOperation(input)
                {
                    InstanceId = context.OrchestrationInstance.InstanceId,
                    ExecutionId = context.OrchestrationInstance.ExecutionId,
                    Stage = ProvisioningStage.StartProvisioning,
                    Input = DataConverter.Serialize(input)
                });
            }

            #region InjectBeforeDeployment

            if (infrastructure.InjectBeforeDeployment)
            {
                try
                {
                    var injectBeforeDeploymenteResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                             RequestOrchestration<T>.Name,
                             "1.0",
                             new AsyncRequestActivityInput()
                             {
                                 InstanceId = context.OrchestrationInstance.InstanceId,
                                 ExecutionId = context.OrchestrationInstance.ExecutionId,
                                 ProvisioningStage = ProvisioningStage.InjectBeforeDeployment
                             });
                    if (injectBeforeDeploymenteResult.Code != 200)
                    {
                        helper.SaveDeploymentOperation(new DeploymentOperation(input)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.BeforeDeploymentFailed,
                            Result = DataConverter.Serialize(injectBeforeDeploymenteResult)
                        });
                        return injectBeforeDeploymenteResult;
                    }
                }
                catch (TaskFailedException ex)
                {
                    var response = new ErrorResponse()
                    {
                        Code = $"{DeploymentOrchestration<T>.Name}:{ProvisioningStage.InjectBeforeDeployment}",
                        Message = ex.Message,
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                    };
                    helper.SafeSaveDeploymentOperation(new DeploymentOperation(input)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.InjectBeforeDeploymentFailed,
                        Input = DataConverter.Serialize(input),
                        Result = DataConverter.Serialize(response)
                    });
                    return new TaskResult()
                    {
                        Code = 500,
                        Content = response
                    };
                }

            }

            #endregion InjectBeforeDeployment

            #region Before Deployment

            if (infrastructure.BeforeDeploymentOrchestration != null)
            {
                foreach (var t in infrastructure.BeforeDeploymentOrchestration)
                {
                    var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                    if (r.Code != 200)
                    {
                        // doesnot SafeSaveDeploymentOperation, should SafeSaveDeploymentOperation in plugin orchestration
                        return r;
                    }

                    input = r.Content as Deployment;
                    input.ServiceProvider = _ServiceProvider;
                }
            }

            #endregion Before Deployment

            #region DependsOn

            if (input.DependsOn.Count > 0)
            {
                waitHandler = new TaskCompletionSource<string>();
                try
                {
                    await context.ScheduleTask<TaskResult>(WaitDependsOnActivity.Name, "1.0",
                                     new WaitDependsOnActivityInput()
                                     {
                                         DependsOn = input.DependsOn.ToList(),
                                         DeploymentId = input.DeploymentId,
                                         RootId = input.RootId,
                                         InstanceId = context.OrchestrationInstance.InstanceId,
                                         ExecutionId = context.OrchestrationInstance.ExecutionId,
                                         DeploymentOperation = new DeploymentOperation(input)
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
                        Code = $"{DeploymentOrchestration<T>.Name}:{ProvisioningStage.DependsOnWaited}",
                        Message = ex.Message,
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                    };
                    helper.SafeSaveDeploymentOperation(new DeploymentOperation(input)
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
                await waitHandler.Task;
                var r = DataConverter.Deserialize<TaskResult>(waitHandler.Task.Result);
                if (r.Code != 200)
                {
                    helper.SaveDeploymentOperation(new DeploymentOperation(input)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.DependsOnWaitedFailed,
                        Result = DataConverter.Serialize(r)
                    });
                    return r;
                }
            }

            #endregion DependsOn

            #region Provisioning resources

            List<Task<TaskResult>> tasks = new List<Task<TaskResult>>();
            List<ErrorResponse> errorResponses = new List<ErrorResponse>();
            foreach (var resource in input.Template.Resources)
            {
                if (!resource.Condition)
                    continue;
                // copy should be executed before BuiltinServiceTypes.Deployments
                // because BuiltinServiceTypes.Deployments can be a copy resource
                if (resource.Copy != null)
                {
                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(CopyOrchestration<T>.Name, "1.0", new ResourceOrchestrationInput()
                    {
                        DeploymentResourceId = input.ResourceId,
                        NameWithServiceType = resource.Copy.NameWithServiceType
                    }));
                }
                else if (resource.Type == infrastructure.BuiltinServiceTypes.Deployments)
                {
                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                        DeploymentOrchestration<T>.Name,
                        "1.0",
                        DataConverter.Serialize(Deployment.Parse(resource))));
                }
                else
                {
                    helper.ProvisioningResource<T>(resource, tasks, context, input);
                }
            }

            await Task.WhenAll(tasks.ToArray());
            foreach (var t in tasks)
            {
                helper.ParseTaskResult(Name, errorResponses, t);
            }

            #endregion Provisioning resources

            input.IsRuntime = true;
            string rtv = null;

            #region After Deployment

            if (infrastructure.AfterDeploymentOrhcestration != null)
            {
                foreach (var t in infrastructure.AfterDeploymentOrhcestration)
                {
                    try
                    {
                        var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                        if (r.Code != 200)
                        {
                            // doesnot SafeSaveDeploymentOperation, should SafeSaveDeploymentOperation in plugin orchestration
                            errorResponses.Add(r.Content as ErrorResponse);
                        }
                        else
                        {
                            input = r.Content as Deployment;
                            input.ServiceProvider = _ServiceProvider;
                        }
                    }
                    catch (TaskFailedException ex)
                    {
                        var response = new ErrorResponse()
                        {
                            Code = $"{Name}:{ProvisioningStage.AfterDeploymentOrhcestration}",
                            Message = ex.Message,
                            Details = errorResponses.ToArray(),
                            AdditionalInfo = new ErrorAdditionalInfo[] {
                                new ErrorAdditionalInfo() {
                                    Type=typeof(TaskFailedException).FullName,
                                    Info=ex
                                }
                            }
                        };
                        helper.SafeSaveDeploymentOperation(new DeploymentOperation(input)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.AfterDeploymentOrhcestrationFailed,
                            Input = DataConverter.Serialize(input),
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

            #endregion After Deployment

            if (infrastructure.InjectAfterDeployment)
            {
                try
                {
                    var injectAfterDeploymenteResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                             RequestOrchestration<T>.Name,
                             "1.0",
                             new AsyncRequestActivityInput()
                             {
                                 InstanceId = context.OrchestrationInstance.InstanceId,
                                 ExecutionId = context.OrchestrationInstance.ExecutionId,
                                 ProvisioningStage = ProvisioningStage.InjectAfterDeployment
                             });
                    if (injectAfterDeploymenteResult.Code != 200)
                    {
                        errorResponses.Add(injectAfterDeploymenteResult.Content as ErrorResponse);
                    }
                }
                catch (TaskFailedException ex)
                {
                    var response = new ErrorResponse()
                    {
                        Code = $"{Name}:{ProvisioningStage.InjectAfterDeployment}",
                        Message = ex.Message,
                        Details = errorResponses.ToArray(),
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                            new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                    };
                    helper.SafeSaveDeploymentOperation(new DeploymentOperation(input)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.InjectAfterDeploymentFailed,
                        Input = DataConverter.Serialize(input),
                        Result = DataConverter.Serialize(response)
                    });
                    return new TaskResult()
                    {
                        Code = 500,
                        Content = response
                    };
                }
            }
            ErrorResponse errorResponse = null;

            #region get template outputs
            if (errorResponses.Count > 0)
            {
                errorResponse = new ErrorResponse()
                {
                    Code = $"{Name}-Provisioning-Failed",
                    Message = $"{Name}-Provisioning-Failed",
                    Details = errorResponses.ToArray()
                };
            }
            else if (input.Template.Outputs != null)
            {
                try
                {
                    rtv = input.GetOutputs();
                }
                catch (Exception ex)
                {
                    errorResponse = new ErrorResponse()
                    {
                        Code = $"{Name}-GetOutputs-Faild",
                        Message = "exception when get outputs",
                        AdditionalInfo = new ErrorAdditionalInfo[] { new ErrorAdditionalInfo() { Type = ex.GetType().FullName, Info = ex } }
                    };
                }
            }

            #endregion get template outputs

            helper.SaveDeploymentOperation(new DeploymentOperation(input)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = errorResponse == null ? ProvisioningStage.Successed : ProvisioningStage.Failed,
                Result = errorResponse == null ? rtv : DataConverter.Serialize(errorResponses)
            });
            if (errorResponse == null)
                return new TaskResult(200, rtv);
            else
                return new TaskResult(500, errorResponse);
        }

        private TaskCompletionSource<string> waitHandler = null;

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.waitHandler != null && name == ProvisioningStage.DependsOnWaited.ToString() && this.waitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.waitHandler.SetResult(input);
            }
        }
    }
}