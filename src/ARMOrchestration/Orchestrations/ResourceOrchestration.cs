using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestration : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        public static string Name { get { return "ResourceOrchestration"; } }
        private readonly IInfrastructure infrastructure;

        private readonly ARMTemplateHelper templateHelper;

        public ResourceOrchestration(
            IInfrastructure infrastructure,
            ARMTemplateHelper templateHelper)
        {
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
                await context.ScheduleTask<TaskResult>(WaitDependsOnActivity.Name, "1.0",
                    new WaitDependsOnActivityInput()
                    {
                        ProvisioningStage = ProvisioningStage.DependsOnWaited,
                        DeploymentContext = input.Context,
                        Resource = resourceDeploy,
                        DependsOn = resourceDeploy.DependsOn
                    });
                await dependsOnWaitHandler.Task;
            }

            #endregion DependsOn

            if (resourceDeploy.Type != Copy.ServiceType)
            {
                if (infrastructure.InjectBefroeProvisioning)
                {
                    var injectBefroeProvisioningResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                 RequestOrchestration.Name,
                                 "1.0",
                                 new AsyncRequestActivityInput()
                                 {
                                     ProvisioningStage = ProvisioningStage.InjectBefroeProvisioning,
                                     DeploymentContext = input.Context,
                                     Resource = resourceDeploy
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
                            return r;
                        input = DataConverter.Deserialize<ResourceOrchestrationInput>(r.Content);
                    }
                }

                #region Provisioning Resource

                var createResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                              RequestOrchestration.Name,
                              "1.0",
                              new AsyncRequestActivityInput()
                              {
                                  InstanceId = context.OrchestrationInstance.InstanceId,
                                  ExecutionId = context.OrchestrationInstance.ExecutionId,
                                  ProvisioningStage = ProvisioningStage.ProvisioningResource,
                                  DeploymentContext = input.Context,
                                  Resource = resourceDeploy
                              });
                if (createResourceResult.Code != 200)
                {
                    return createResourceResult;
                }

                #endregion Provisioning Resource
            }

            #region wait for child resource completed

            if (resourceDeploy.Resources.Count > 0)
            {
                childWaitHandler = new TaskCompletionSource<string>();
                await context.ScheduleTask<TaskResult>(WaitDependsOnActivity.Name, "1.0",
                    new WaitDependsOnActivityInput()
                    {
                        ProvisioningStage = ProvisioningStage.WaitChildCompleted,
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
                            RequestOrchestration.Name,
                            "1.0",
                             new AsyncRequestActivityInput()
                             {
                                 InstanceId = context.OrchestrationInstance.InstanceId,
                                 ExecutionId = context.OrchestrationInstance.ExecutionId,
                                 ProvisioningStage = ProvisioningStage.CreateExtensionResource,
                                 DeploymentContext = input.Context,
                                 Resource = resourceDeploy,
                                 Context = new Dictionary<string, object>() {
                                    {"extenstion",item.Value }
                                }
                             }
                            ));
                }
                if (extenstionTasks.Count != 0)
                {
                    await Task.WhenAll(extenstionTasks);
                    int succeed = 0;
                    int failed = 0;
                    List<string> extensionResult = new List<string>();
                    foreach (var t in extenstionTasks)
                    {
                        if (t.Result.Code == 200) succeed++;
                        else failed++;
                        extensionResult.Add(t.Result.Content);
                    }
                    if (failed > 0)
                    {
                        await context.ScheduleTask<TaskResult>(DeploymentOperationActivity.Name, "1.0",
                            new DeploymentOperation(input.Context, infrastructure, resourceDeploy)
                            {
                                InstanceId = context.OrchestrationInstance.InstanceId,
                                ExecutionId = context.OrchestrationInstance.ExecutionId,
                                Stage = ProvisioningStage.CreateExtensionResource,
                                Comments = $"Extension resource succeed/total: {succeed}/{extenstionTasks.Count}",
                                Result = $"[{string.Join(',', extensionResult)}]"
                            });
                        return new TaskResult() { Code = 500, Content = $"Extension resource succeed/total: {succeed}/{extenstionTasks.Count}" };
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

                if (infrastructure.InjectAfterProvisioning)
                {
                    if (infrastructure.InjectBeforeDeployment)
                    {
                        var injectAfterProvisioningResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                     RequestOrchestration.Name,
                                     "1.0",
                                     new AsyncRequestActivityInput()
                                     {
                                         ProvisioningStage = ProvisioningStage.InjectAfterProvisioning,
                                         DeploymentContext = input.Context,
                                         Resource = resourceDeploy
                                     });
                        if (injectAfterProvisioningResult.Code != 200)
                        {
                            return injectAfterProvisioningResult;
                        }
                    }
                }
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

        private TaskCompletionSource<string> dependsOnWaitHandler = null;
        private TaskCompletionSource<string> childWaitHandler = null;

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.dependsOnWaitHandler != null && name == ProvisioningStage.DependsOnWaited.ToString() && this.dependsOnWaitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.dependsOnWaitHandler.SetResult(input);
            }
            else if (this.childWaitHandler != null && name == ProvisioningStage.WaitChildCompleted.ToString() && this.childWaitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.childWaitHandler.SetResult(input);
            }
        }
    }
}