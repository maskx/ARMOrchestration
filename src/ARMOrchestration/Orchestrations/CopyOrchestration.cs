using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Threading.Tasks;
using static maskx.ARMOrchestration.Activities.DeploymentOperationsActivityInput;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class CopyOrchestration : TaskOrchestration<TaskResult, CopyOrchestrationInput>
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, CopyOrchestrationInput input)
        {
            var operationArgs = new DeploymentOperationsActivityInput()
            {
                //DeploymentId = input.DeploymentId,
                //InstanceId = context.OrchestrationInstance.InstanceId,
                //ExecutionId = context.OrchestrationInstance.ExecutionId,
                //CorrelationId = input.CorrelationId,
                //Resource = resourceDeploy.Name,
                //Type = resourceDeploy.Type,
                //ResourceId = resourceDeploy.ResouceId,
                //ParentId = input.Parent?.ResourceId,
                Stage = ProvisioningStage.StartProcessing
            };
            if (input.Copy.Mode == Copy.SerialMode)
            {
                foreach (var item in input.Copy.Resources)
                {
                    await context.CreateSubOrchestrationInstance<TaskResult>(
                        typeof(ResourceOrchestration),
                        new ResourceOrchestrationInput()
                        {
                        });
                }
            }
            else // TODO: support batchSize
            {
                var loopTask = new List<Task>();

                foreach (var item in input.Copy.Resources)
                {
                    loopTask.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                        typeof(ResourceOrchestration),
                        new ResourceOrchestrationInput()
                        {
                        }));
                }

                await Task.WhenAll(loopTask.ToArray());
            }
            operationArgs.Stage = ProvisioningStage.Successed;
            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);

            return new TaskResult() { Code = 200 };
        }
    }
}