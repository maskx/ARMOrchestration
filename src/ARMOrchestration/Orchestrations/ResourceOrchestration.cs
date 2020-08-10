using DurableTask.Core;
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

            #region DependsOn

            if (input.Resource.DependsOn.Count > 0)
            {
                dependsOnWaitHandler = new TaskCompletionSource<string>();
                await context.ScheduleTask<TaskResult>(WaitDependsOnActivity.Name, "1.0",
                    new WaitDependsOnActivityInput()
                    {
                        DependsOn = input.Resource.DependsOn.ToList(),
                        DeploymentId = input.Input.DeploymentId,
                        RootId = input.Input.RootId,
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        DeploymentOperation = new DeploymentOperation(input.Input, infrastructure, input.Resource)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId
                        }
                    });
                await dependsOnWaitHandler.Task;
                var r = DataConverter.Deserialize<TaskResult>(dependsOnWaitHandler.Task.Result);
                if (r.Code != 200)
                {
                    templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Input, infrastructure, input.Resource)
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
                var injectBefroeProvisioningResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                             RequestOrchestration.Name,
                             "1.0",
                             new AsyncRequestActivityInput()
                             {
                                 InstanceId = context.OrchestrationInstance.InstanceId,
                                 ExecutionId = context.OrchestrationInstance.ExecutionId,
                                 ProvisioningStage = ProvisioningStage.InjectBefroeProvisioning,
                                 Resource = input.Resource,
                                 Input = input.Input
                             });
                if (injectBefroeProvisioningResult.Code != 200)
                {
                    return injectBefroeProvisioningResult;
                }
            }

            if (infrastructure.BeforeResourceProvisioningOrchestation != null)
            {
                foreach (var t in infrastructure.BeforeResourceProvisioningOrchestation)
                {
                    var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                    if (r.Code != 200)
                    {
                        templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Input, infrastructure, input.Resource)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.BeforeResourceProvisioningFailed,
                            Input = DataConverter.Serialize(input),
                            Result = DataConverter.Serialize(r)
                        });
                        return r;
                    }

                    input = DataConverter.Deserialize<ResourceOrchestrationInput>(r.Content);
                }
            }

            #endregion plug-in

            #region Provisioning Resource

            var createResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                      RequestOrchestration.Name,
                      "1.0",
                      new AsyncRequestActivityInput()
                      {
                          InstanceId = context.OrchestrationInstance.InstanceId,
                          ExecutionId = context.OrchestrationInstance.ExecutionId,
                          ProvisioningStage = ProvisioningStage.ProvisioningResource,
                          Input = input.Input,
                          Resource = input.Resource
                      });
            if (createResourceResult.Code != 200)
            {
                return createResourceResult;
            }

            #endregion Provisioning Resource

            #region After Resource Provisioning

            if (infrastructure.AfterResourceProvisioningOrchestation != null)
            {
                foreach (var t in infrastructure.AfterResourceProvisioningOrchestation)
                {
                    var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                    if (r.Code != 200)
                    {
                        templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Input, infrastructure, input.Resource)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.AfterResourceProvisioningOrchestationFailed,
                            Input = DataConverter.Serialize(input),
                            Result = DataConverter.Serialize(r)
                        });
                        return r;
                    }

                    input = DataConverter.Deserialize<ResourceOrchestrationInput>(r.Content);
                }
            }

            #endregion After Resource Provisioning

            if (infrastructure.InjectAfterProvisioning)
            {
                if (infrastructure.InjectBeforeDeployment)
                {
                    var injectAfterProvisioningResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                 RequestOrchestration.Name,
                                 "1.0",
                                 new AsyncRequestActivityInput()
                                 {
                                     InstanceId = context.OrchestrationInstance.InstanceId,
                                     ExecutionId = context.OrchestrationInstance.ExecutionId,
                                     ProvisioningStage = ProvisioningStage.InjectAfterProvisioning,
                                     Input = input.Input,
                                     Resource = input.Resource
                                 });
                    if (injectAfterProvisioningResult.Code != 200)
                    {
                        return injectAfterProvisioningResult;
                    }
                }
            }

            #region Ready Resource

            templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Input, infrastructure, input.Resource)
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