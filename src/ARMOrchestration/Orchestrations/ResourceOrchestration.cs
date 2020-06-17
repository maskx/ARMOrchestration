using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Text.Json;
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

            #region Evaluate functions

            // if there no dependson, means that there no implicit dependency by reference function
            // so there no need re-evaluate functions
            if (!string.IsNullOrEmpty(resourceDeploy.Properties) && resourceDeploy.DependsOn.Count > 0)
            {
                var doc = JsonDocument.Parse(resourceDeploy.Properties);
                Dictionary<string, object> cxt = new Dictionary<string, object>() { { ContextKeys.ARM_CONTEXT, input.Context } };
                if (!string.IsNullOrEmpty(resourceDeploy.CopyName))
                {
                    cxt.Add(ContextKeys.CURRENT_LOOP_NAME, resourceDeploy.CopyName);
                    cxt.Add(ContextKeys.COPY_INDEX, new Dictionary<string, int>() { { resourceDeploy.CopyName, resourceDeploy.CopyIndex } });
                }
                resourceDeploy.Properties = doc.RootElement.ExpandObject(cxt, templateHelper);
            }

            #endregion Evaluate functions

            #region plug-in

            if (resourceDeploy.Type != Copy.ServiceType)
            {
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
                        {
                            templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Context, infrastructure, resourceDeploy)
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
            }

            #endregion plug-in

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
                var r = DataConverter.Deserialize<TaskResult>(dependsOnWaitHandler.Task.Result);
                if (r.Code != 200)
                {
                    templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Context, infrastructure, resourceDeploy)
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

            #region Provisioning Resource

            if (resourceDeploy.Type != Copy.ServiceType)
            {
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
            }

            #endregion Provisioning Resource

            if (resourceDeploy.Type != Copy.ServiceType)
            {
                #region After Resource Provisioning

                if (infrastructure.AfterResourceProvisioningOrchestation != null)
                {
                    foreach (var t in infrastructure.AfterResourceProvisioningOrchestation)
                    {
                        var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                        if (r.Code != 200)
                        {
                            templateHelper.SaveDeploymentOperation(new DeploymentOperation(input.Context, infrastructure, resourceDeploy)
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

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.dependsOnWaitHandler != null && name == ProvisioningStage.DependsOnWaited.ToString() && this.dependsOnWaitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.dependsOnWaitHandler.SetResult(input);
            }
        }
    }
}