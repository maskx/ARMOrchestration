using DurableTask.Core;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Orchestration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.OrchestrationCreator.Orchestrations
{
    public class ARMOrchestration : TaskOrchestration<TaskResult, string>
    {
        private ResourceOrchestrationOptions resourceOptions;
        private ARMOrchestrationOptions options;

        public ARMOrchestration(
            IOptions<ARMOrchestrationOptions> options,
            IOptions<ResourceOrchestrationOptions> resourceOptions)
        {
            this.options = options?.Value;
            this.resourceOptions = resourceOptions?.Value;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, string arg)
        {
            ARMOrchestrationInput input = this.DataConverter.Deserialize<ARMOrchestrationInput>(arg);
            string rtv = string.Empty;
            Dictionary<string, object> armContext = new Dictionary<string, object>();
            armContext.Add("armcontext", input);
            var template = new ARMTemplate.Template(input.Template, armContext);

            if (input.Mode.ToLower() == "complete")
            {
            }
            else
            {
                #region ResourceGroup ReadOnly Lock Check

                if (resourceOptions.GetLockCheckRequestInput != null)
                {
                    TaskResult readonlyLockCheckResult;

                    if (template.DeployLevel == ARMTemplate.Template.ResourceGroupDeploymentLevel)
                    {
                        readonlyLockCheckResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                       typeof(AsyncRequestOrchestration),
                                       resourceOptions.GetLockCheckRequestInput(
                                           ARMFunctions.Evaluate($"[subscriptionresourceid('{options.BuitinServiceTypes.ResourceGroup}','{input.ResourceGroup}')]", armContext).ToString(),
                                           "readonly"));
                    }
                    else if (template.DeployLevel == ARMTemplate.Template.SubscriptionDeploymentLevel)
                    {
                        readonlyLockCheckResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                      typeof(AsyncRequestOrchestration),
                                      resourceOptions.GetLockCheckRequestInput(
                                          ARMFunctions.Evaluate($"[tenantresourceid('{options.BuitinServiceTypes.Subscription}','{input.SubscriptionId}')]", armContext).ToString(),
                                          "readonly"));
                    }
                    else
                    {
                        //there no Tenant level lock
                        readonlyLockCheckResult = new TaskResult() { Code = 404 };
                    }
                    if (readonlyLockCheckResult.Code != 404)
                        return readonlyLockCheckResult;
                }

                #endregion ResourceGroup ReadOnly Lock Check

                List<Task> tasks = new List<Task>();

                foreach (var resource in template.Resources)
                {
                    if (null == resource.Copy)
                    {
                        var p = new ResourceOrchestrationInput()
                        {
                            Resource = resource.ToString(),
                            OrchestrationContext = armContext
                        };
                        tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), p));
                    }
                    else
                    {
                        var copy = resource.Copy;
                        var loopName = copy.Name;
                        var loopCount = copy.Count;
                        var copyindex = new Dictionary<string, int>()
                    {
                        { loopName,0 }
                    };
                        Dictionary<string, object> copyContext = new Dictionary<string, object>();
                        copyContext.Add("armcontext", input);
                        copyContext.Add("copyindex", copyindex);
                        copyContext.Add("currentloopname", loopName);
                        for (int i = 0; i < loopCount; i++)
                        {
                            copyindex[loopName] = i;
                            var par = new ResourceOrchestrationInput()
                            {
                                Resource = resource.ToString(),
                                OrchestrationContext = copyContext
                            };
                            tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), par));
                        }
                    }
                }
                await Task.WhenAll(tasks.ToArray());

                if (!string.IsNullOrEmpty(template.Outputs))
                {
                    rtv = ARMFunctions.GetOutputs(template.Outputs, armContext);
                }
            }
            return new TaskResult() { Code = 200, Content = rtv };
        }
    }
}