using DurableTask.Core;
using maskx.OrchestrationCreator.Activity;
using maskx.OrchestrationCreator.ARMTemplate;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using maskx.OrchestrationService.Orchestration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator
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

            if (resourceDeploy.DependsOn != null)
            {
                dependsOnwaitHandler = new TaskCompletionSource<string>();
                await context.ScheduleTask<string>(typeof(WaitDependsOnActivity), "");
                await dependsOnwaitHandler.Task;
            }

            #endregion DependsOn

            #region check policy

            var checkPolicyResult = await context.ScheduleTask<TaskResult>("IARMPolicy.Check", "", "123");
            if (checkPolicyResult.Code != 200)
            {
                return checkPolicyResult;
            }

            #endregion check policy

            #region Begin Create Resource

            var beginResourceResult = await context.ScheduleTask<TaskResult>("IResource.Begin", "");
            if (beginResourceResult.Code != 200)
            {
                return beginResourceResult;
            }

            #endregion Begin Create Resource

            #region Begin Quota

            var beginQuotaResult = await context.ScheduleTask<TaskResult>("IQuota.Begin", "");
            if (beginQuotaResult.Code != 200)
            {
                return beginQuotaResult;
            }

            #endregion Begin Quota

            #region Create or Update Resource

            Dictionary<string, object> ruleField = new Dictionary<string, object>();
            ruleField.Add("ApiVersion", ARMFunctions.Evaluate(resourceDeploy.ApiVersion, input.OrchestrationContext));
            ruleField.Add("Type", ARMFunctions.Evaluate(resourceDeploy.Type, input.OrchestrationContext));
            ruleField.Add("Name", ARMFunctions.Evaluate(resourceDeploy.Name, input.OrchestrationContext));
            ruleField.Add("Location", ARMFunctions.Evaluate(resourceDeploy.Location, input.OrchestrationContext));
            ruleField.Add("SKU", resourceDeploy.SKU);
            ruleField.Add("Kind", ARMFunctions.Evaluate(resourceDeploy.Kind, input.OrchestrationContext));
            ruleField.Add("Plan", resourceDeploy.Plan);
            AsyncRequestInput asyncRequestInput = new AsyncRequestInput()
            {
                RequestTo = "ResourceProvider",// TODO: 支持Subscription level Resource和Tenant level Resource后，将有不同的ResourceTo
                RequestOperation = "PUT",//ResourceProvider 处理 Create Or Update
                RequsetContent = resourceDeploy.Properties,
                RuleField = ruleField,
                Processor = this.options.RPCommunicationProcessorName
            };
            var response = await context.CreateSubOrchestrationInstance<TaskResult>(
                 typeof(AsyncRequestOrchestration),
                 asyncRequestInput);
            if (response.Code != 200)
                return response;

            #endregion Create or Update Resource

            #region Commit Quota

            await context.ScheduleTask<TaskResult>("IQuota.Commit", "");

            #endregion Commit Quota

            #region Commit Resource

            await context.ScheduleTask<TaskResult>("IResource.Commit", "");

            #endregion Commit Resource

            #region Apply Policy

            var applyPolicyResult = await context.ScheduleTask<TaskResult>("IARMPolicy.Apply", "", string.Empty);
            if (applyPolicyResult.Code != 200)
            {
                return checkPolicyResult;
            }

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