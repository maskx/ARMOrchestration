using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Orchestration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestration : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        private readonly ARMOrchestrationOptions ARMOptions;
        private readonly IServiceProvider serviceProvider;
        private ARMFunctions functions;

        public ResourceOrchestration(
            IServiceProvider serviceProvider,
            IOptions<ARMOrchestrationOptions> armOptions,
            ARMFunctions functions)
        {
            this.ARMOptions = armOptions?.Value;
            this.serviceProvider = serviceProvider;
            this.functions = functions;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            var resourceDeploy = input.Resource;

            var operationArgs = new DeploymentOperationsActivityInput()
            {
                DeploymentId = input.Context.DeploymentId,
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                CorrelationId = input.Context.CorrelationId,
                Resource = resourceDeploy.Name,
                Type = resourceDeploy.Type,
                ResourceId = resourceDeploy.ResouceId,
                ParentId = input.ParentId,
                Stage = ProvisioningStage.StartProcessing
            };

            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);

            #region Condition

            if (resourceDeploy.Condition)
            {
                operationArgs.Stage = ProvisioningStage.ConditionCheckSuccessed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            }
            else
            {
                var conditionFail = new TaskResult() { Code = 200, Content = "condition is false" };
                operationArgs.Stage = ProvisioningStage.ConditionCheckFailed;
                operationArgs.Result = this.DataConverter.Serialize(conditionFail);
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                return conditionFail;
            }

            #endregion Condition

            #region DependsOn

            if (null != resourceDeploy.DependsOn && resourceDeploy.DependsOn.Count > 0)
            {
                operationArgs.Stage = ProvisioningStage.DependsOnWaited;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                await context.CreateSubOrchestrationInstance<TaskResult>(
                    typeof(WaitDependsOnOrchestration),
                    (input.Context.RootId, resourceDeploy.DependsOn));
                operationArgs.Stage = ProvisioningStage.DependsOnSuccessed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            }

            #endregion DependsOn

            #region check policy

            var checkPolicyResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                            typeof(RequestOrchestration),
                            new RequestOrchestrationInput()
                            {
                                RequestAction = RequestAction.CheckPolicy,
                                DeploymentContext = input.Context,
                                Resource = resourceDeploy
                            });
            if (checkPolicyResult.Code == 200)
            {
                operationArgs.Stage = ProvisioningStage.PolicyCheckSuccessed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            }
            else
            {
                operationArgs.Stage = ProvisioningStage.PolicyCheckFailed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                return checkPolicyResult;
            }

            #endregion check policy

            TaskResult beginCreateResourceResult = null;
            if (resourceDeploy.Type != ARMOptions.BuitinServiceTypes.Deployments)
            {
                #region Check Resource

                beginCreateResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                typeof(RequestOrchestration),
                new RequestOrchestrationInput()
                {
                    DeploymentContext = input.Context,
                    Resource = resourceDeploy,
                    RequestAction = RequestAction.CheckResource
                });
                // In communication service
                // TODO: when resource in Provisioning, we need wait
                // communication should return the resource status until  resource  available to be Provisioning
                // TODO: should check authorization.
                // communication should return 503 when no authorization
                if (beginCreateResourceResult.Code == 200 || beginCreateResourceResult.Code == 204)
                {
                    operationArgs.Stage = ProvisioningStage.ResourceCheckSuccessed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                }
                else
                {
                    operationArgs.Stage = ProvisioningStage.ResourceCheckFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    return beginCreateResourceResult;
                }

                #endregion Check Resource

                #region Resource ReadOnly Lock Check

                // code=200 update; code=204 create
                if (beginCreateResourceResult.Code == 200)
                {
                    TaskResult readonlyLockCheckResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                       typeof(RequestOrchestration),
                                       new RequestOrchestrationInput()
                                       {
                                           Resource = resourceDeploy,
                                           DeploymentContext = input.Context,
                                           RequestAction = RequestAction.CheckLock
                                       });
                    if (readonlyLockCheckResult.Code == 404)// lock not exist
                    {
                        operationArgs.Stage = ProvisioningStage.LockCheckSuccessed;
                        await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    }
                    else
                    {
                        operationArgs.Stage = ProvisioningStage.LockCheckFailed;
                        await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                        return readonlyLockCheckResult;
                    }
                }

                #endregion Resource ReadOnly Lock Check

                #region Check Quota

                var checkQoutaResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                 typeof(RequestOrchestration),
                 new RequestOrchestrationInput()
                 {
                 });
                if (checkQoutaResult.Code == 200)
                {
                    operationArgs.Stage = ProvisioningStage.QuotaCheckSuccessed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                }
                else
                {
                    operationArgs.Stage = ProvisioningStage.QuotaCheckFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    return checkQoutaResult;
                }

                #endregion Check Quota
            }

            #region Create or Update Resource

            TaskResult createResourceResult = null;
            if (resourceDeploy.Type == ARMOptions.BuitinServiceTypes.Deployments)
            {
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/cross-resource-group-deployment?tabs=azure-powershell
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/linked-templates
                // deployment history is different to the Azure
                // we nest the history of the nest deployment template
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/linked-templates#deployment-history
                Dictionary<string, object> armContext = new Dictionary<string, object>() {
                    { "armcontext",input.Context}
                };
                using var doc = JsonDocument.Parse(resourceDeploy.Properties);
                var rootElement = doc.RootElement;
                var mode = DeploymentMode.Incremental;
                if (rootElement.TryGetProperty("mode", out JsonElement _mode)
                    && _mode.GetString().Equals(DeploymentMode.Complete.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    mode = DeploymentMode.Complete;
                }
                string template = string.Empty;
                if (rootElement.TryGetProperty("template", out JsonElement _template))
                {
                    template = _template.GetRawText();
                }
                TemplateLink templateLink = null;
                if (rootElement.TryGetProperty("templateLink", out JsonElement _templateLink))
                {
                    templateLink = new TemplateLink()
                    {
                        ContentVersion = _templateLink.GetProperty("contentVersion").GetString(),
                        Uri = this.functions.Evaluate(_templateLink.GetProperty("uri").GetString(), armContext).ToString()
                    };
                }
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/linked-templates#scope-for-expressions-in-nested-templates
                string parameters = string.Empty;
                ParametersLink parametersLink = null;
                if (rootElement.TryGetProperty("expressionEvaluationOptions", out JsonElement _expressionEvaluationOptions)
                    && _expressionEvaluationOptions.GetProperty("scope").GetString().Equals("inner", StringComparison.OrdinalIgnoreCase))
                {
                    if (rootElement.TryGetProperty("parameters", out JsonElement _parameters))
                    {
                        parameters = _parameters.GetRawText();
                    }
                    if (rootElement.TryGetProperty("parametersLink", out JsonElement _parametersLink))
                    {
                        parametersLink = new ParametersLink()
                        {
                            ContentVersion = _parametersLink.GetProperty("contentVersion").GetString(),
                            Uri = this.functions.Evaluate(_parametersLink.GetProperty("uri").GetString(), armContext).ToString()
                        };
                    }
                }
                else
                {
                    parameters = input.Context.Parameters;

                    var jobj = JObject.Parse(rootElement.GetRawText());
                    jobj["variables"] = input.Context.Template.Variables;
                }

                createResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                    typeof(DeploymentOrchestration),
                    DataConverter.Serialize(
                    new DeploymentOrchestrationInput()
                    {
                        CorrelationId = input.Context.CorrelationId,
                        Name = resourceDeploy.Name,
                        SubscriptionId = resourceDeploy.SubscriptionId,
                        ResourceGroup = resourceDeploy.ResourceGroup,
                        Mode = mode,
                        Template = template,
                        TemplateLink = templateLink,
                        Parameters = parameters,
                        ParametersLink = parametersLink
                    }));
            }
            else
            {
                createResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                              typeof(RequestOrchestration),
                              new RequestOrchestrationInput()
                              {
                                  RequestAction = beginCreateResourceResult.Code == 200 ? RequestAction.UpdateResource : RequestAction.CreateResource,
                                  DeploymentContext = input.Context,
                                  Resource = resourceDeploy
                              });
            }

            if (createResourceResult.Code == 200)
            {
                operationArgs.Stage = ProvisioningStage.ResourceCreateSuccessed;
                operationArgs.Result = createResourceResult.Content;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            }
            else
            {
                operationArgs.Stage = ProvisioningStage.ResourceCreateFailed;
                operationArgs.Result = createResourceResult.Content;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                return createResourceResult;
            }

            #endregion Create or Update Resource

            if (resourceDeploy.Type != ARMOptions.BuitinServiceTypes.Deployments)
            {
                #region Extension Resources

                List<Task<TaskResult>> extenstionTasks = new List<Task<TaskResult>>();
                foreach (var item in resourceDeploy.ExtensionResource)
                {
                    extenstionTasks.Add(
                        context.CreateSubOrchestrationInstance<TaskResult>(
                            typeof(RequestOrchestration),
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
                        await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                        return new TaskResult() { Code = 500, Content = operationArgs.Result };
                    }
                    else
                    {
                        operationArgs.Stage = ProvisioningStage.ExtensionResourceSuccessed;
                        await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    }
                }

                #endregion Extension Resources

                #region Create Or Update child resource

                if (resourceDeploy.Resources != null)
                {
                    List<Task<TaskResult>> childTasks = new List<Task<TaskResult>>();
                    foreach (var r in resourceDeploy.Resources)
                    {
                        var p = new ResourceOrchestrationInput()
                        {
                            Resource = r,
                        };
                        childTasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                            typeof(ResourceOrchestration),
                            p));
                        // ARM does NOT support Iteration for a child resource
                        // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances#iteration-for-a-child-resource
                        //if (null == r.Copy)
                        //{
                        //    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), p));
                        //}
                        //else
                        //{
                        //    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(GroupOrchestration), p));
                        //}
                    }
                    await Task.WhenAll(childTasks);
                    int successed = 0;
                    int failed = 0;
                    foreach (var t in childTasks)
                    {
                        if (t.Result.Code == 200) successed++;
                        else failed++;
                    }
                    if (failed > 0)
                    {
                        operationArgs.Stage = ProvisioningStage.ChildResourceFailed;
                        operationArgs.Result = $"Child resource successed: {successed}/{childTasks.Count} ";
                        await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                        return new TaskResult() { Code = 500, Content = operationArgs.Result };
                    }
                    else
                    {
                        operationArgs.Stage = ProvisioningStage.ChildResourceSuccessed;
                        await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    }
                }

                #endregion Create Or Update child resource

                #region Apply Policy

                // TODO: should start a orchestration
                // in communication
                var applyPolicyResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                  typeof(RequestOrchestration),
                  new RequestOrchestrationInput()
                  {
                      RequestAction = RequestAction.ApplyPolicy,
                      Resource = resourceDeploy,
                      DeploymentContext = input.Context
                  });
                if (applyPolicyResult.Code == 200)
                {
                    operationArgs.Stage = ProvisioningStage.PolicyApplySuccessed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                }
                else
                {
                    operationArgs.Stage = ProvisioningStage.PolicyApplyFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    return applyPolicyResult;
                }

                #endregion Apply Policy

                #region Commit Quota

                var commitQoutaResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                typeof(RequestOrchestration),
                new RequestOrchestrationInput()
                {
                    RequestAction = RequestAction.CommitQuota,
                    Resource = resourceDeploy,
                    DeploymentContext = input.Context
                });
                if (commitQoutaResult.Code == 200)
                {
                    operationArgs.Stage = ProvisioningStage.QuotaCommitSuccesed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                }
                else
                {
                    operationArgs.Stage = ProvisioningStage.QuotaCommitFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
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
                typeof(RequestOrchestration),
                new RequestOrchestrationInput()
                {
                    Resource = resourceDeploy,
                    RequestAction = RequestAction.CommitResource,
                    DeploymentContext = input.Context
                });
                if (commitResourceResult.Code == 200)
                {
                    operationArgs.Stage = ProvisioningStage.ResourceCommitSuccessed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                }
                else
                {
                    operationArgs.Stage = ProvisioningStage.ResourceCommitFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    return commitResourceResult;
                }

                #endregion Commit Resource
            }

            #region save deployment result

            operationArgs.Stage = ProvisioningStage.Successed;
            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);

            #endregion save deployment result

            return new TaskResult() { Code = 200 };
        }
    }
}