using DurableTask.Core;
using maskx.OrchestrationCreator.Activity;
using maskx.OrchestrationCreator.ARMTemplate;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Orchestration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator.Orchestrations
{
    public class ResourceOrchestration : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        private const string dependsOnEventName = "DependsOn";
        private TaskCompletionSource<string> dependsOnwaitHandler = null;
        public ResourceOrchestrationOptions options;

        public ResourceOrchestration(IOptions<ResourceOrchestrationOptions> options)
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
                dependsOnwaitHandler = new TaskCompletionSource<string>();
                await context.ScheduleTask<string>(typeof(WaitDependsOnOrchestration), (resourceDeploy.DependsOn, input.OrchestrationContext));
                await dependsOnwaitHandler.Task;
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
                    if (null == r.Copy)
                    {
                        var p = new ResourceOrchestrationInput()
                        {
                            Resource = r.ToString(),
                            OrchestrationContext = input.OrchestrationContext
                        };
                        tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), p));
                    }
                    else
                    {
                        var copy = r.Copy;
                        var loopName = copy.Name;
                        var loopCount = copy.Count;
                        var copyindex = new Dictionary<string, int>()
                    {
                        { loopName,0 }
                    };
                        Dictionary<string, object> copyContext = new Dictionary<string, object>();
                        copyContext.Add("armcontext", input.OrchestrationContext);
                        copyContext.Add("copyindex", copyindex);
                        copyContext.Add("currentloopname", loopName);
                        for (int i = 0; i < loopCount; i++)
                        {
                            copyindex[loopName] = i;
                            var par = new ResourceOrchestrationInput()
                            {
                                Resource = r.ToString(),
                                OrchestrationContext = copyContext
                            };
                            tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), par));
                        }
                    }
                }
                await Task.WhenAll(tasks.ToArray());
            }

            #endregion Create Or Update child resource

            return new TaskResult() { Code = 200 };
        }

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.dependsOnwaitHandler != null && name == dependsOnEventName)
            {
                this.dependsOnwaitHandler.SetResult(input);
            }
            base.OnEvent(context, name, input);
        }
    }
}