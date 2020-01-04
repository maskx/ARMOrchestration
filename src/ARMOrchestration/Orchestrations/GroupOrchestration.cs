using DurableTask.Core;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class GroupOrchestration : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            var resource = new Resource(input.Resource, input.OrchestrationContext);
            var copy = resource.Copy;
            var loopName = copy.Name;
            var loopCount = copy.Count;
            var copyindex = new Dictionary<string, int>() { { loopName, 0 } };
            Dictionary<string, object> copyContext = new Dictionary<string, object>();
            copyContext.Add("armcontext", input.OrchestrationContext);
            copyContext.Add("copyindex", copyindex);
            copyContext.Add("currentloopname", loopName);
            if (copy.Mode == Copy.SerialMode)
            {
                for (int i = 0; i < loopCount; i++)
                {
                    copyindex[loopName] = i;
                    var par = new ResourceOrchestrationInput()
                    {
                        Resource = resource.ToString(),
                        OrchestrationContext = copyContext,
                        DeploymentId = input.DeploymentId,
                        Parent = new ResourceOrchestrationInput.ParentResource()
                        {
                            Resource = loopName,
                            Type = "copy",
                            ResourceId = $"deployment/{input.DeploymentId}/copy/{loopName}"
                        }
                    };
                    await context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), par);
                }
            }
            else // TODO: support batchSize
            {
                var loopTask = new List<Task>();

                for (int i = 0; i < loopCount; i++)
                {
                    copyindex[loopName] = i;

                    var par = new ResourceOrchestrationInput()
                    {
                        Resource = resource.ToString(),
                        OrchestrationContext = copyContext,
                        DeploymentId = input.DeploymentId,
                        Parent = new ResourceOrchestrationInput.ParentResource()
                        {
                            Resource = loopName,
                            Type = "copy",
                            ResourceId = $"deployment/{input.DeploymentId}/copy/{loopName}"
                        }
                    };

                    loopTask.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), par));
                }

                await Task.WhenAll(loopTask.ToArray());
            }

            return new TaskResult() { Code = 200 };
        }
    }
}