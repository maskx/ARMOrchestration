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
        protected readonly ARMTemplateHelper helper;
        protected readonly IInfrastructure infrastructure;
        protected readonly IServiceProvider _ServiceProvider;
        private string _DeploymentOperationId;
        Deployment _Deployment;
        Deployment input
        {
            get
            {
                if (_Deployment == null)
                {
                    _Deployment = helper.GetInputAsync<Deployment>(_DeploymentOperationId).Result;
                    _Deployment.ServiceProvider = _ServiceProvider;
                    _Deployment.IsRuntime = true;
                }
                return _Deployment;
            }
        }
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
            if (arg.StartsWith('{'))
            {
                ResourceOrchestrationInput res = DataConverter.Deserialize<ResourceOrchestrationInput>(arg);
                res.ServiceProvider = this._ServiceProvider;
                if (!res.IsRetry)
                    _DeploymentOperationId = context.OrchestrationInstance.InstanceId;
                if (!context.IsReplaying)
                {
                    var dep = Deployment.Parse(res.Resource);
                    var _ = dep.Template.Variables;
                    dep.IsRetry = res.IsRetry;
                    if (res.IsRetry)
                    {
                        var r = helper.PrepareRetry(res.DeploymentOperationId, context.OrchestrationInstance.InstanceId, context.OrchestrationInstance.ExecutionId, res.LastRunUserId, DataConverter.Serialize(dep));
                        if (r == null)
                            return new TaskResult(400, new ErrorResponse()
                            {
                                Code = $"{Name}:PrepareRetry",
                                Message = $"cannot find DeploymentOperation with Id:{res.DeploymentOperationId}"
                            });
                        if (r.Value == ProvisioningStage.Successed)
                            return new TaskResult(200, "");
                        if (r.Value != ProvisioningStage.StartProvisioning)
                            return new TaskResult(400, new ErrorResponse()
                            {
                                Code = $"{Name}:PrepareRetry",
                                Message = $"Deployment[{_DeploymentOperationId}] in stage of [{r.Value}], only failed deployment support retry"
                            });
                    }
                    else
                    {
                        dep.DeploymentId = context.OrchestrationInstance.InstanceId;
                        var operation = helper.CreatDeploymentOperation(new DeploymentOperation(_DeploymentOperationId, dep)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.StartProvisioning,
                            Input = DataConverter.Serialize(dep)
                        }).Result;
                        if (operation == null)
                            return new TaskResult(400, new ErrorResponse()
                            {
                                Code = $"{Name}:CreatDeploymentOperation",
                                Message = "CorrelationId duplicated"
                            });
                        if (operation.Id != _DeploymentOperationId)
                            return new TaskResult(400, new ErrorResponse()
                            {
                                Code = $"{Name}:CreatDeploymentOperation",
                                Message = $"{operation.ResourceId} already exists"
                            });
                    }
                }
            }
            else
            {
                _DeploymentOperationId = arg;
                if (!context.IsReplaying)
                {
                    if (input.IsRetry)
                    {
                        var r = helper.PrepareRetry(_DeploymentOperationId, context.OrchestrationInstance.InstanceId, context.OrchestrationInstance.ExecutionId, input.LastRunUserId);
                        if (r == null)
                            return new TaskResult(400, new ErrorResponse()
                            {
                                Code = $"{Name}:PrepareRetry",
                                Message = $"cannot find DeploymentOperation with Id:{_DeploymentOperationId}"
                            });
                        if (r.Value == ProvisioningStage.Successed)
                            return new TaskResult(200, "");
                        if (r.Value != ProvisioningStage.StartProvisioning)
                            return new TaskResult(400, new ErrorResponse()
                            {
                                Code = $"{Name}:PrepareRetry",
                                Message = $"Deployment[{_DeploymentOperationId}] in stage of [{r.Value}], only failed deployment support retry"
                            });
                    }
                    else
                    {
                        helper.SaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId, input)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.StartProvisioning
                        });
                    }
                }
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
                                 DeploymentOperationId = _DeploymentOperationId,
                                 ProvisioningStage = ProvisioningStage.InjectBeforeDeployment
                             });
                    if (injectBeforeDeploymenteResult.Code != 200)
                    {
                        helper.SaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId)
                        {
                            Stage = ProvisioningStage.InjectBeforeDeploymentFailed,
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
                    helper.SafeSaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId)
                    {
                        Stage = ProvisioningStage.InjectBeforeDeploymentFailed,
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
                try
                {
                    foreach (var t in infrastructure.BeforeDeploymentOrchestration)
                    {
                        var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, _DeploymentOperationId);
                        if (r.Code != 200)
                        {
                            helper.SafeSaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId)
                            {
                                Stage = ProvisioningStage.BeforeDeploymentFailed,
                                Result = DataConverter.Serialize(r)
                            });
                            return r;
                        }
                    }
                }
                catch (TaskFailedException ex)
                {
                    var response = new ErrorResponse()
                    {
                        Code = $"{DeploymentOrchestration<T>.Name}:{ProvisioningStage.BeforeDeployment}",
                        Message = ex.Message,
                        AdditionalInfo = new ErrorAdditionalInfo[] {
                        new ErrorAdditionalInfo() {
                            Type=typeof(TaskFailedException).FullName,
                            Info=ex
                        } }
                    };
                    helper.SafeSaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId)
                    {
                        Stage = ProvisioningStage.BeforeDeploymentFailed,
                        Result = DataConverter.Serialize(response)
                    });
                    return new TaskResult()
                    {
                        Code = 500,
                        Content = response
                    };
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
                                         DeploymentOperationId = _DeploymentOperationId,
                                         DependsOn = input.DependsOn.ToList(),
                                         DeploymentId = input.DeploymentId,
                                         RootId = input.RootId
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
                    helper.SafeSaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId)
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
                await waitHandler.Task;
                var r = DataConverter.Deserialize<TaskResult>(waitHandler.Task.Result);
                if (r.Code != 200)
                {
                    helper.SaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId)
                    {
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
                helper.ProvisioningResource<T>(resource, tasks, context, input.IsRetry, input.LastRunUserId);
            }

            await Task.WhenAll(tasks.ToArray());
            foreach (var t in tasks)
            {
                helper.ParseTaskResult(Name, errorResponses, t);
            }

            #endregion Provisioning resources

            string rtv = null;

            #region After Deployment

            if (infrastructure.AfterDeploymentOrhcestration != null)
            {
                foreach (var t in infrastructure.AfterDeploymentOrhcestration)
                {
                    try
                    {
                        var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, _DeploymentOperationId);
                        if (r.Code != 200)
                        {
                            helper.SafeSaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId)
                            {
                                Stage = ProvisioningStage.AfterDeploymentOrhcestrationFailed
                            });
                            errorResponses.Add(r.Content as ErrorResponse);
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
                        helper.SafeSaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId)
                        {
                            Stage = ProvisioningStage.AfterDeploymentOrhcestrationFailed,
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
                                 DeploymentOperationId = _DeploymentOperationId,
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
                    helper.SafeSaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId)
                    {
                        Stage = ProvisioningStage.InjectAfterDeploymentFailed,
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

            helper.SaveDeploymentOperation(new DeploymentOperation(_DeploymentOperationId)
            {
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