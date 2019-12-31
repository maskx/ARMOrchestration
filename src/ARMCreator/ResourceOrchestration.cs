using DurableTask.Core;
using maskx.OrchestrationCreator.Activity;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Activity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator
{
    public class ResourceOrchestration : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        private const string dependsOnEventName = "DependsOn";
        private TaskCompletionSource<string> dependsOnwaitHandler = null;
        private const string createResourceEventName = "createResource";
        private TaskCompletionSource<string> createResourceWaitHandler = null;

        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            var resourceDeploy = input.Resource;

            #region Condition

            if (null != resourceDeploy.Condition)
            {
                if (resourceDeploy.Condition is bool b)
                {
                    if (!b) return new TaskResult() { Code = 200, Content = "condition is false" };
                }
                else if (resourceDeploy.Condition is string s)
                {
                    var c = ARMFunctions.Evaluate(s, input.OrchestrationContext);
                    if (c is bool b1 && !b1)
                        return new TaskResult() { Code = 200, Content = $"condition {s} evaluate result is {c}" };
                }
            }

            #endregion Condition

            #region DependsOn

            if (resourceDeploy.DependsOn.Count > 0)
            {
                dependsOnwaitHandler = new TaskCompletionSource<string>();
                await context.ScheduleTask<string>(typeof(SetDependsOnActivity), "");
                await dependsOnwaitHandler.Task;
            }

            #endregion DependsOn

            // TODO: check policy

            // TODO: 获取资源信息
            // 如果资源已存在，operation 为 PUT
            string requestOperation = "POST";

            // TODO:配额检查

            #region Create or Update Resource

            createResourceWaitHandler = new TaskCompletionSource<string>();
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
                EventName = createResourceEventName,
                RequestTo = "ResourceProvider",// TODO: 支持Subscription level Resource和Tenant level Resource后，将有不同的ResourceTo
                RequestOperation = requestOperation,
                RequsetContent = resourceDeploy.Properties,
                RuleField = ruleField
            };
            await context.ScheduleTask<TaskResult>(typeof(AsyncRequestActivity), asyncRequestInput);
            await createResourceWaitHandler.Task;
            var result = DataConverter.Deserialize<TaskResult>(createResourceWaitHandler.Task.Result);
            // TODO: 提交资产
            // TODO: 提交配额
            // TODO: resourceDeploy.Tags
            // TODO: 更新关联资源：如删除NIC，要更新SLB的后端地址池

            #endregion Create or Update Resource

            #region Create Or Update child resource

            List<Task> tasks = new List<Task>();
            foreach (var r in resourceDeploy.Resources)
            {
                if (null == r.Copy)
                {
                    var p = new ResourceOrchestrationInput()
                    {
                        Resource = r,
                        OrchestrationContext = input.OrchestrationContext
                    };
                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), p));
                }
                else
                {
                    var copy = r.Copy;
                    var loopName = ARMFunctions.Evaluate(copy.Name, input.OrchestrationContext).ToString();
                    var loopCount = (int)ARMFunctions.Evaluate(copy.Count, input.OrchestrationContext);
                    var copyindex = new Dictionary<string, int>()
                    {
                        { loopName,0 }
                    };
                    Dictionary<string, object> copyContext = new Dictionary<string, object>();
                    copyContext.Add("armcontext", input.OrchestrationContext);
                    copyContext.Add("copyindex", copyindex);
                    copyContext.Add("copyindexcurrentloopname", copy.Name);
                    for (int i = 0; i < loopCount; i++)
                    {
                        copyindex[loopName] = i;
                        var par = new ResourceOrchestrationInput()
                        {
                            Resource = r,
                            OrchestrationContext = copyContext
                        };
                        tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), par));
                    }
                }
            }
            await Task.WhenAll(tasks.ToArray());

            #endregion Create Or Update child resource

            // TODO: Apply Policy

            return new TaskResult() { Code = 201 };
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