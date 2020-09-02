using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestration : TaskOrchestration<TaskResult, string>
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
            DeploymentOrchestrationInput input = this.DataConverter.Deserialize<DeploymentOrchestrationInput>(arg);
            input.ServiceProvider = this._ServiceProvider;
            if (string.IsNullOrEmpty(input.RootId))
            {
                input.RootId = input.DeploymentId;
            }

            #region InjectBeforeDeployment

            if (infrastructure.InjectBeforeDeployment)
            {
                var injectBeforeDeploymenteResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                             RequestOrchestration.Name,
                             "1.0",
                             new AsyncRequestActivityInput()
                             {
                                 InstanceId = context.OrchestrationInstance.InstanceId,
                                 ExecutionId = context.OrchestrationInstance.ExecutionId,
                                 ProvisioningStage = ProvisioningStage.InjectBeforeDeployment,

                                 Resource = null
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

            #endregion InjectBeforeDeployment

            #region Before Deployment

            if (infrastructure.BeforeDeploymentOrchestration != null)
            {
                foreach (var t in infrastructure.BeforeDeploymentOrchestration)
                {
                    var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                    if (r.Code != 200)
                    {
                        helper.SaveDeploymentOperation(new DeploymentOperation(input)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.BeforeDeploymentFailed,
                            Result = DataConverter.Serialize(r)
                        });
                        return r;
                    }

                    input = DataConverter.Deserialize<DeploymentOrchestrationInput>(r.Content);
                    input.ServiceProvider = _ServiceProvider;
                }
            }

            #endregion Before Deployment

            #region DependsOn

            if (input.DependsOn.Count > 0)
            {
                waitHandler = new TaskCompletionSource<string>();
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

            bool hasFailResource = false;

            #region Provisioning resources

            List<Task<TaskResult>> tasks = new List<Task<TaskResult>>();

            foreach (var resource in input.Template.Resources)
            {
                if (!resource.Condition)
                    continue;
                // copy should be executed before BuiltinServiceTypes.Deployments
                // because BuiltinServiceTypes.Deployments can be a copy resource
                if (resource.Copy != null)
                {
                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(CopyOrchestration.Name, "1.0", new ResourceOrchestrationInput()
                    {
                        Resource = resource,
                        Input = input
                    }));
                }
                else if (resource.Type == infrastructure.BuiltinServiceTypes.Deployments)
                {
                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                        DeploymentOrchestration.Name,
                        "1.0",
                        DataConverter.Serialize(DeploymentOrchestrationInput.Parse(resource))));
                }
                else
                {
                    helper.ProvisioningResource(resource, tasks, context, input);
                }
            }

            await Task.WhenAll(tasks.ToArray());
            foreach (var t in tasks)
            {
                if (t.Result.Code != 200)
                {
                    hasFailResource = true;
                    break;
                }
            }

            #endregion Provisioning resources

            input.IsRuntime = true;
            string rtv = null;

            #region After Deployment

            if (infrastructure.AfterDeploymentOrhcestration != null)
            {
                foreach (var t in infrastructure.AfterDeploymentOrhcestration)
                {
                    var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                    if (r.Code != 200)
                    {
                        helper.SaveDeploymentOperation(new DeploymentOperation(input)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.AfterDeploymentOrhcestrationFailed,
                            Result = DataConverter.Serialize(r)
                        });
                        return r;
                    }
                    input = DataConverter.Deserialize<DeploymentOrchestrationInput>(r.Content);
                    input.ServiceProvider = _ServiceProvider;
                }
            }

            #endregion After Deployment

            if (infrastructure.InjectAfterDeployment)
            {
                var injectAfterDeploymenteResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                             RequestOrchestration.Name,
                             "1.0",
                             new AsyncRequestActivityInput()
                             {
                                 InstanceId = context.OrchestrationInstance.InstanceId,
                                 ExecutionId = context.OrchestrationInstance.ExecutionId,
                                 ProvisioningStage = ProvisioningStage.InjectAfterDeployment,
                                 Input = input,
                                 Resource = null
                             });
                if (injectAfterDeploymenteResult.Code != 200)
                {
                    return injectAfterDeploymenteResult;
                }
            }

            #region get template outputs

            if (input.Template.Outputs != null)
            {
                try
                {
                    rtv = input.GetOutputs();
                }
                catch (Exception ex)
                {
                    hasFailResource = true;
                    rtv = ex.Message;
                }
            }

            #endregion get template outputs

            helper.SaveDeploymentOperation(new DeploymentOperation(input)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = hasFailResource ? ProvisioningStage.Failed : ProvisioningStage.Successed,
                Result = rtv
            });
            return new TaskResult() { Code = hasFailResource ? 500 : 200, Content = rtv };
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