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

        public ResourceOrchestration(
            IServiceProvider serviceProvider,
            IOptions<ARMOrchestrationOptions> armOptions,
            ARMFunctions functions,
            IInfrastructure infrastructure)
        {
            this.ARMOptions = armOptions?.Value;
            this.serviceProvider = serviceProvider;
            this.functions = functions;
            this.infrastructure = infrastructure;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            var resourceDeploy = input.Resource;
            var operationArgs = new DeploymentOperationActivityInput()
            {
                DeploymentContext = input.Context,
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Name = resourceDeploy.Name,
                Type = resourceDeploy.Type,
                ResourceId = resourceDeploy.ResouceId,
                ParentId = string.IsNullOrEmpty(resourceDeploy.CopyId) ? input.Context.DeploymentId : resourceDeploy.CopyId,
                Stage = ProvisioningStage.StartProcessing,
                Input = DataConverter.Serialize(input.Resource)
            };

            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);

            #region DependsOn

            if (resourceDeploy.DependsOn.Count > 0)
            {
                operationArgs.Stage = ProvisioningStage.DependsOnWaited;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
                await context.CreateSubOrchestrationInstance<TaskResult>(
                    typeof(WaitDependsOnOrchestration).Name,
                    "1.0",
                    (input.Context.DeploymentId, resourceDeploy.DependsOn));
                operationArgs.Stage = ProvisioningStage.DependsOnSuccessed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
            }

            #endregion DependsOn

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

            #region Create or Update Resource

            if (resourceDeploy.Type != Copy.ServiceType)
            {
                TaskResult createResourceResult = null;
                if (resourceDeploy.Type == this.infrastructure.BuitinServiceTypes.Deployments)
                {
                    Debugger.Break();
                }
                else
                {
                    createResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                  typeof(RequestOrchestration).Name,
                                  "1.0",
                                  new RequestOrchestrationInput()
                                  {
                                      RequestAction = RequestAction.CreateResource,
                                      DeploymentContext = input.Context,
                                      Resource = resourceDeploy
                                  });
                }

                if (createResourceResult.Code == 200)
                {
                    operationArgs.Stage = ProvisioningStage.ResourceCreateSuccessed;
                    operationArgs.Result = DataConverter.Deserialize<CommunicationResult>(createResourceResult.Content).ResponseContent;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
                }
                else
                {
                    operationArgs.Stage = ProvisioningStage.ResourceCreateFailed;
                    operationArgs.Result = DataConverter.Deserialize<CommunicationResult>(createResourceResult.Content).ResponseContent;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
                    return createResourceResult;
                }
            }

            #endregion Create or Update Resource

            #region wait for child resource completed

            if (resourceDeploy.Resources.Count > 0)
            {
                await context.CreateSubOrchestrationInstance<TaskResult>(
                    typeof(WaitDependsOnOrchestration).Name,
                    "1.0",
                    (input.Context.DeploymentId, resourceDeploy.Resources));
                operationArgs.Stage = ProvisioningStage.ChildResourceSuccessed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
            }

            #endregion wait for child resource completed

            if (resourceDeploy.Type != this.infrastructure.BuitinServiceTypes.Deployments
                && resourceDeploy.Type != Copy.ServiceType)
            {
                #region Commit Quota

                var commitQoutaResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                typeof(RequestOrchestration).Name,
                "1.0",
                new RequestOrchestrationInput()
                {
                    RequestAction = RequestAction.CommitQuota,
                    Resource = resourceDeploy,
                    DeploymentContext = input.Context
                });
                if (commitQoutaResult.Code == 200)
                {
                    operationArgs.Stage = ProvisioningStage.QuotaCommitSuccesed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
                }
                else
                {
                    operationArgs.Stage = ProvisioningStage.QuotaCommitFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
                    return commitQoutaResult;
                }

                #endregion Commit Quota

                #region Commit Resource

                // TODO: 需要考考虑一个RP请求创建了多个资源的情况，怎样提交到资产服务
                // 创建虚机的请求里直接新建网卡和磁盘，怎样在资产服务里同时记录虚机、磁盘、网卡的情况
                // 创建VNet时 创建的subnet，添加的ACL
                // 同时创建资源的情形，RP返回的propeties里应该包含创建的所有资源信息（可以是资源的ID）
                ///ARM 不了解 资源的Properties，直接转发给资产服务
                // 资产服务 里有 资源之间的关系，知道 具体资源可以包含其他资源，及其这些被包含的资源在报文中的路径，从而可以进行处理

                var commitResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                    typeof(RequestOrchestration).Name,
                    "1.0",
                    new RequestOrchestrationInput()
                    {
                        Resource = resourceDeploy,
                        RequestAction = RequestAction.CommitResource,
                        DeploymentContext = input.Context
                    });
                if (commitResourceResult.Code == 200)
                {
                    operationArgs.Stage = ProvisioningStage.ResourceCommitSuccessed;
                    operationArgs.Result = commitResourceResult.Content;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
                }
                else
                {
                    operationArgs.Stage = ProvisioningStage.ResourceCommitFailed;
                    operationArgs.Result = commitQoutaResult.Content;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
                    return commitResourceResult;
                }

                #endregion Commit Resource
            }

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
                    operationArgs.Stage = ProvisioningStage.ExtensionResourceFailed;
                    operationArgs.Result = $"Extension resource successed: {successed}/{extenstionTasks.Count} ";
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
                    return new TaskResult() { Code = 500, Content = operationArgs.Result };
                }
                else
                {
                    operationArgs.Stage = ProvisioningStage.ExtensionResourceSuccessed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
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

            #region Ready Resource

            var readyResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                typeof(RequestOrchestration).Name,
                "1.0",
                new RequestOrchestrationInput()
                {
                    Resource = resourceDeploy,
                    RequestAction = RequestAction.ReadyResource,
                    DeploymentContext = input.Context
                });
            if (readyResourceResult.Code == 200)
            {
                operationArgs.Stage = ProvisioningStage.Successed;
                operationArgs.Result = DataConverter.Deserialize<CommunicationResult>(readyResourceResult.Content).ResponseContent;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
            }
            else
            {
                operationArgs.Stage = ProvisioningStage.ResourceReadyFailed;
                operationArgs.Result = DataConverter.Deserialize<CommunicationResult>(readyResourceResult.Content).ResponseContent;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
                return readyResourceResult;
            }

            #endregion Ready Resource

            return new TaskResult() { Code = 200 };
        }
    }
}