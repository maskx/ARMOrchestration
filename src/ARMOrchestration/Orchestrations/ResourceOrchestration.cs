using DurableTask.Core;
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

            #region Condition

            if (!resourceDeploy.Condition)
                return new TaskResult() { Code = 200, Content = "condition is false" };

            #endregion Condition

            #region DependsOn

            if (!string.IsNullOrEmpty(resourceDeploy.DependsOn))
            {
                await context.ScheduleTask<string>(typeof(WaitDependsOnOrchestration), (resourceDeploy.DependsOn, input.OrchestrationContext));
            }

            #endregion DependsOn

            #region check policy

            if (options.GetCheckPolicyRequestInput != null)
            {
                var checkPolicyResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                typeof(AsyncRequestOrchestration),
                                options.GetCheckPolicyRequestInput(input));
                if (checkPolicyResult.Code != 200)
                    return checkPolicyResult;
            }

            #endregion check policy

            TaskResult beginCreateResourceResult = null;

            #region Begin Create Resource

            if (options.GetBeginCreateResourceRequestInput != null)
            {
                beginCreateResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                typeof(AsyncRequestOrchestration),
                options.GetBeginCreateResourceRequestInput(input));
                if (!(beginCreateResourceResult.Code == 200 || beginCreateResourceResult.Code == 204))
                    return beginCreateResourceResult;
            }

            #endregion Begin Create Resource

            #region Resource ReadOnly Lock Check

            // code=200 update; code=204 create
            if (beginCreateResourceResult != null && beginCreateResourceResult.Code == 200)
            {
                if (options.GetLockCheckRequestInput != null)
                {
                    TaskResult readonlyLockCheckResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                       typeof(AsyncRequestOrchestration),
                                       options.GetLockCheckRequestInput(
                                           ARMFunctions.Evaluate($"[resourceid('{resourceDeploy.Type}','{resourceDeploy.Name}')]", input.OrchestrationContext).ToString(),
                                           "readonly"));
                    if (readonlyLockCheckResult.Code != 404)
                        return readonlyLockCheckResult;
                }
            }

            #endregion Resource ReadOnly Lock Check

            #region Begin Quota

            if (options.GetCheckQoutaRequestInput != null)
            {
                var checkQoutaResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                 typeof(AsyncRequestOrchestration),
                 options.GetCheckQoutaRequestInput(input));
                if (checkQoutaResult.Code != 200)
                    return checkQoutaResult;
            }

            #endregion Begin Quota

            #region Create or Update Resource
            // TODO: support property-iteration
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances#property-iteration
            var createResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                 typeof(AsyncRequestOrchestration),
                 options.GetCreateResourceRequestInput(input));
            if (createResourceResult.Code != 200)
                return createResourceResult;

            #endregion Create or Update Resource

            #region Commit Quota

            if (options.GetCommitQoutaRequestInput != null)
            {
                var commitQoutaResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                typeof(AsyncRequestOrchestration),
                options.GetCommitQoutaRequestInput(input));
                if (commitQoutaResult.Code != 200)
                    return commitQoutaResult;
            }

            #endregion Commit Quota

            #region Commit Resource

            if (options.GetCommitResourceRequestInput != null)
            {
                var commitResourceResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                typeof(AsyncRequestOrchestration),
                options.GetCommitResourceRequestInput(input));
                if (commitResourceResult.Code != 200)
                    return commitResourceResult;
            }

            #endregion Commit Resource

            // TODO: save deployment status to resolve dependson resource

            #region extension resource

            // TODO: extension resource.such as tags

            #endregion extension resource

            #region Apply Policy

            // TODO: should start a orchestration

            #endregion Apply Policy

            #region Create Or Update child resource

            if (resourceDeploy.Resources != null)
            {
                List<Task> tasks = new List<Task>();
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
            }

            #endregion Create Or Update child resource

            #region
            // TODO: save deployment result
            #endregion
            return new TaskResult() { Code = 200 };
        }
    }
}