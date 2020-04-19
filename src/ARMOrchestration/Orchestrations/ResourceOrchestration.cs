using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestration : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        private readonly ARMOrchestrationOptions ARMOptions;
        private readonly IServiceProvider serviceProvider;
        private readonly ARMFunctions functions;
        private readonly IInfrastructure infrastructure;
        private readonly ARMTemplateHelper templateHelper;

        public ResourceOrchestration(
            IServiceProvider serviceProvider,
            IOptions<ARMOrchestrationOptions> armOptions,
            ARMFunctions functions,
            IInfrastructure infrastructure,
            ARMTemplateHelper templateHelper)
        {
            this.ARMOptions = armOptions?.Value;
            this.serviceProvider = serviceProvider;
            this.functions = functions;
            this.infrastructure = infrastructure;
            this.templateHelper = templateHelper;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            var resourceDeploy = input.Resource;

            #region DependsOn

            if (resourceDeploy.DependsOn.Count > 0)
            {
                dependsOnWaitHandler = new TaskCompletionSource<string>();
                await context.ScheduleTask<TaskResult>(typeof(WaitDependsOnActivity).Name, "1.0",
                    new WaitDependsOnActivityInput()
                    {
                        EventName = dependsOnEventName,
                        DeploymentContext = input.Context,
                        Resource = resourceDeploy,
                        DependsOn = resourceDeploy.DependsOn
                    });
                await dependsOnWaitHandler.Task;
            }
            else
            {
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", new DeploymentOperation(input.Context, infrastructure, resourceDeploy)
                {
                    InstanceId = context.OrchestrationInstance.InstanceId,
                    ExecutionId = context.OrchestrationInstance.ExecutionId,
                    Stage = ProvisioningStage.StartProcessing,
                    Input = DataConverter.Serialize(input)
                });
            }

            #endregion DependsOn

            if (resourceDeploy.Type != Copy.ServiceType)
            {
                #region Before Resource Provisioning

                if (infrastructure.BeforeResourceProvisioningOrchestation != null)
                {
                    foreach (var t in infrastructure.BeforeResourceProvisioningOrchestation)
                    {
                        var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                        if (r.Code != 200)
                            return r;
                        input = DataConverter.Deserialize<ResourceOrchestrationInput>(r.Content);
                    }
                }

                #endregion Before Resource Provisioning

                #region Provisioning Resource

                TaskResult createResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                              typeof(RequestOrchestration).Name,
                              "1.0",
                              new RequestOrchestrationInput()
                              {
                                  RequestAction = RequestAction.ProvisioningResource,
                                  DeploymentContext = input.Context,
                                  Resource = resourceDeploy
                              });

                if (createResourceResult.Code == 200)
                {
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0",
                     new DeploymentOperation(input.Context, infrastructure, resourceDeploy)
                     {
                         InstanceId = context.OrchestrationInstance.InstanceId,
                         ExecutionId = context.OrchestrationInstance.ExecutionId,
                         Stage = ProvisioningStage.ResourceCreateSuccessed,
                         Result = DataConverter.Deserialize<CommunicationResult>(createResourceResult.Content).ResponseContent
                     });
                }
                else
                {
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0",
                    new DeploymentOperation(input.Context, infrastructure, resourceDeploy)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.ResourceCreateFailed,
                        Result = DataConverter.Deserialize<CommunicationResult>(createResourceResult.Content).ResponseContent
                    });
                    return createResourceResult;
                }

                #endregion Provisioning Resource
            }

            #region wait for child resource completed

            if (resourceDeploy.Resources.Count > 0)
            {
                childWaitHandler = new TaskCompletionSource<string>();
                await context.ScheduleTask<TaskResult>(typeof(WaitDependsOnActivity).Name, "1.0",
                    new WaitDependsOnActivityInput()
                    {
                        EventName = childEventName,
                        DeploymentContext = input.Context,
                        Resource = resourceDeploy,
                        DependsOn = resourceDeploy.Resources
                    });
                await childWaitHandler.Task;
            }

            #endregion wait for child resource completed

            if (resourceDeploy.Type != Copy.ServiceType)
            {
                #region Extension Resources

                List<Task<TaskResult>> extenstionTasks = new List<Task<TaskResult>>();
                foreach (var item in resourceDeploy.ExtensionResource)
                {
                    extenstionTasks.Add(
                        context.CreateSubOrchestrationInstance<TaskResult>(
                            typeof(RequestOrchestration).Name,
                            "1.0",
                            new RequestOrchestrationInput()
                            {
                                Resource = resourceDeploy,
                                RequestAction = RequestAction.CreateExtensionResource,
                                DeploymentContext = input.Context,
                                Context = new Dictionary<string, object>() {
                                {"extenstion",item.Value }
                                }
                            }));
                }
                if (extenstionTasks.Count != 0)
                {
                    await Task.WhenAll(extenstionTasks);
                    int successed = 0;
                    int failed = 0;
                    foreach (var t in extenstionTasks)
                    {
                        if (t.Result.Code == 200) successed++;
                        else failed++;
                    }
                    if (failed > 0)
                    {
                        await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0",
                            new DeploymentOperation(input.Context, infrastructure, resourceDeploy)
                            {
                                InstanceId = context.OrchestrationInstance.InstanceId,
                                ExecutionId = context.OrchestrationInstance.ExecutionId,
                                Stage = ProvisioningStage.ExtensionResourceFailed,
                                Result = $"Extension resource successed: {successed}/{extenstionTasks.Count}"
                            });
                        return new TaskResult() { Code = 500, Content = $"Extension resource successed: {successed}/{extenstionTasks.Count}" };
                    }
                    else
                    {
                        await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0",
                           new DeploymentOperation(input.Context, infrastructure, resourceDeploy)
                           {
                               InstanceId = context.OrchestrationInstance.InstanceId,
                               ExecutionId = context.OrchestrationInstance.ExecutionId,
                               Stage = ProvisioningStage.ExtensionResourceSuccessed
                           });
                    }
                }

                #endregion Extension Resources

                #region After Resource Provisioning

                if (infrastructure.AfterResourceProvisioningOrchestation != null)
                {
                    foreach (var t in infrastructure.AfterResourceProvisioningOrchestation)
                    {
                        var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                        if (r.Code != 200)
                            return r;
                        input = DataConverter.Deserialize<ResourceOrchestrationInput>(r.Content);
                    }
                }

                #endregion After Resource Provisioning
            }

            #region Ready Resource

            templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Context, infrastructure, resourceDeploy)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = ProvisioningStage.Successed,
                Input = DataConverter.Serialize(input)
            });

            #endregion Ready Resource

            return new TaskResult() { Code = 200 };
        }

        internal const string dependsOnEventName = "WaitDependsOn";
        private TaskCompletionSource<string> dependsOnWaitHandler = null;
        internal const string childEventName = "child";
        private TaskCompletionSource<string> childWaitHandler = null;

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (name == dependsOnEventName && this.dependsOnWaitHandler != null && this.dependsOnWaitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.dependsOnWaitHandler.SetResult(input);
            }
            if (name == childEventName && this.childWaitHandler != null && this.childWaitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.childWaitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}