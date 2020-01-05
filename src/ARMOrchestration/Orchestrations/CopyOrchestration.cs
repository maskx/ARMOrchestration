using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Threading.Tasks;
using static maskx.ARMOrchestration.Activities.DeploymentOperationsActivityInput;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class CopyOrchestration : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            var resource = new Resource(input.Resource, input.OrchestrationContext);
            var copy = resource.Copy;
            var loopName = copy.Name;
            var loopCount = copy.Count;
            var operationArgs = new DeploymentOperationsActivityInput()
            {
                DeploymentId = input.DeploymentId,
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                CorrelationId = input.CorrelationId,
                Resource = loopName,
                Type = Copy.ServiceType,
                ResourceId = copy.GetId(input.DeploymentId),
                ParentId = input.Parent?.ResourceId,
                Stage = ProvisioningStage.StartProcessing
            };
            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);

            var copyindex = new Dictionary<string, int>() { { loopName, 0 } };
            Dictionary<string, object> copyContext = new Dictionary<string, object>();
            copyContext.Add("armcontext", input.OrchestrationContext["armcontext"]);
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
                        CorrelationId = input.CorrelationId,
                        Parent = new ResourceOrchestrationInput.ParentResource()
                        {
                            Resource = loopName,
                            Type = Copy.ServiceType,
                            ResourceId = copy.GetId(input.DeploymentId)
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
                        CorrelationId = input.CorrelationId,
                        Parent = new ResourceOrchestrationInput.ParentResource()
                        {
                            Resource = loopName,
                            Type = Copy.ServiceType,
                            ResourceId = copy.GetId(input.DeploymentId)
                        }
                    };

                    loopTask.Add(context.CreateSubOrchestrationInstance<TaskResult>(typeof(ResourceOrchestration), par));
                }

                await Task.WhenAll(loopTask.ToArray());
            }
            operationArgs.Stage = ProvisioningStage.Successed;
            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);

            return new TaskResult() { Code = 200 };
        }
    }
}