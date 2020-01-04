using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Activity;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Orchestration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class ResourceOrchestration : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        public TemplateOrchestrationOptions options;

        public ResourceOrchestration(IOptions<TemplateOrchestrationOptions> options)
        {
            this.options = options?.Value;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            var resourceDeploy = new Resource(input.Resource, input.OrchestrationContext);
            var operationArgs = new DeploymentOperationsActivityInput()
            {
                DeploymentId = input.DeploymentId,
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                CorrelationId = input.CorrelationId,
                Resource = resourceDeploy.Name,
                Type = resourceDeploy.Type,
                ResourceId = resourceDeploy.ResouceId,
                ParentId = input.Parent?.ResourceId,
                Stage = DeploymentOperationsActivityInput.ProvisioningStage.StartProcessing
            };
            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);

            #region Condition

            if (resourceDeploy.Condition)
            {
                operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ConditionCheckSuccessed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            }
            else
            {
                operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ConditionCheckFailed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                return new TaskResult() { Code = 200, Content = "condition is false" };
            }

            #endregion Condition

            #region DependsOn

            if (!string.IsNullOrEmpty(resourceDeploy.DependsOn))
            {
                operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.DependsOnWaited;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                await context.ScheduleTask<string>(typeof(WaitDependsOnOrchestration), (resourceDeploy.DependsOn, input.OrchestrationContext));
                operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.DependsOnSuccessed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            }

            #endregion DependsOn

            #region check policy

            if (options.GetCheckPolicyRequestInput != null)
            {
                var checkPolicyResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                typeof(AsyncRequestOrchestration),
                                options.GetCheckPolicyRequestInput(input));
                if (checkPolicyResult.Code == 200)
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.PolicyCheckSuccessed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                }
                else
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.PolicyCheckFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    return checkPolicyResult;
                }
            }

            #endregion check policy

            TaskResult beginCreateResourceResult = null;

            #region Check Resource

            if (options.GetCheckResourceRequestInput != null)
            {
                beginCreateResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                typeof(AsyncRequestOrchestration),
                options.GetCheckResourceRequestInput(input));
                if (beginCreateResourceResult.Code == 200 || beginCreateResourceResult.Code == 204)
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ResourceCheckSuccessed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                }
                else
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ResourceCheckFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    return beginCreateResourceResult;
                }
            }

            #endregion Check Resource

            #region Resource ReadOnly Lock Check

            // code=200 update; code=204 create
            if (beginCreateResourceResult != null && beginCreateResourceResult.Code == 200)
            {
                if (options.GetCheckLockRequestInput != null)
                {
                    TaskResult readonlyLockCheckResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                       typeof(AsyncRequestOrchestration),
                                       options.GetCheckLockRequestInput(
                                           ARMFunctions.Evaluate($"[resourceid('{resourceDeploy.Type}','{resourceDeploy.Name}')]", input.OrchestrationContext).ToString(),
                                           "readonly"));
                    if (readonlyLockCheckResult.Code == 404)// lock not exist
                    {
                        operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.LockCheckSuccessed;
                        await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    }
                    else
                    {
                        operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.LockCheckFailed;
                        await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                        return readonlyLockCheckResult;
                    }
                }
            }

            #endregion Resource ReadOnly Lock Check

            #region Check Quota

            if (options.GetCheckQoutaRequestInput != null)
            {
                var checkQoutaResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                 typeof(AsyncRequestOrchestration),
                 options.GetCheckQoutaRequestInput(input));
                if (checkQoutaResult.Code == 200)
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.QuotaCheckSuccessed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                }
                else
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.QuotaCheckFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    return checkQoutaResult;
                }
            }

            #endregion Check Quota

            #region Create or Update Resource

            // TODO: support property-iteration
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances#property-iteration
            var createResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                 typeof(AsyncRequestOrchestration),
                 options.GetCreateResourceRequestInput(input));
            if (createResourceResult.Code == 200)
            {
                operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ResourceCreateSuccessed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            }
            else
            {
                operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ResourceCreateFailed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                return createResourceResult;
            }

            #endregion Create or Update Resource

            #region Commit Quota

            if (options.GetCommitQoutaRequestInput != null)
            {
                var commitQoutaResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                typeof(AsyncRequestOrchestration),
                options.GetCommitQoutaRequestInput(input));
                if (commitQoutaResult.Code == 200)
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.QuotaCommitSuccesed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                }
                else
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.QuotaCommitFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    return commitQoutaResult;
                }
            }

            #endregion Commit Quota

            #region Commit Resource

            if (options.GetCommitResourceRequestInput != null)
            {
                var commitResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                typeof(AsyncRequestOrchestration),
                options.GetCommitResourceRequestInput(input));
                if (commitResourceResult.Code == 200)
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ResourceCommitSuccessed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                }
                else
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ResourceCommitFailed;
                    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
                    return commitResourceResult;
                }
            }

            #endregion Commit Resource

            #region extension resource

            // TODO: extension resource.such as tags
            //if (.Code == 200)
            //{
            //    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ExtensionResourceSuccessed;
            //    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            //}
            //else
            //{
            //    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ExtensionResourceFailed;
            //    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            //    return commitResourceResult;
            //}

            #endregion extension resource

            #region Apply Policy

            // TODO: should start a orchestration
            //if (.Code == 200)
            //{
            //    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.PolicyApplySuccessed;
            //    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            //}
            //else
            //{
            //    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.PolicyApplyFailed;
            //    await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            //    return commitResourceResult;
            //}

            #endregion Apply Policy

            #region Create Or Update child resource

            if (resourceDeploy.Resources != null)
            {
                List<Task<TaskResult>> tasks = new List<Task<TaskResult>>();
                foreach (var r in resourceDeploy.Resources)
                {
                    var p = new ResourceOrchestrationInput()
                    {
                        Resource = r.ToString(),
                        OrchestrationContext = input.OrchestrationContext
                    };
                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), p));
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
                await Task.WhenAll(tasks.ToArray());
                int successed = 0;
                int failed = 0;
                foreach (var t in tasks)
                {
                    if (t.Result.Code == 200) successed++;
                    else failed++;
                }
                if (failed > 0)
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ChildResourceFailed;
                }
                else
                {
                    operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.ChildResourceSuccessed;
                }
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            }

            #endregion Create Or Update child resource

            #region save deployment result

            operationArgs.Stage = DeploymentOperationsActivityInput.ProvisioningStage.Successed;
            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);

            #endregion save deployment result

            return new TaskResult() { Code = 200 };
        }
    }
}