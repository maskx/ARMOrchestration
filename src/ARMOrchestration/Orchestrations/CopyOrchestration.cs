using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class CopyOrchestration : TaskOrchestration<TaskResult, CopyOrchestrationInput>
    {
        public override async Task<TaskResult> RunTask(OrchestrationContext context, CopyOrchestrationInput input)
        {
            var operationArgs = new DeploymentOperationsActivityInput()
            {
                DeploymentId = input.Context.DeploymentId,
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                CorrelationId = input.Context.CorrelationId,
                Resource = input.Copy.Name,
                Type = Copy.ServiceType,
                ResourceId = input.Copy.Id,
                Stage = ProvisioningStage.StartProcessing
            };
            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationsActivity), operationArgs);
            if (input.Copy.Mode == Copy.SerialMode)
            {
                foreach (var item in input.Copy.Resources)
                {
                    var par = new ResourceOrchestrationInput()
                    {
                        ParentId = input.Copy.Id,
                        Resource = item.Value,
                        Context = input.Context
                    };
                    await context.CreateSubOrchestrationInstance<TaskResult>(
                        typeof(ResourceOrchestration),
                        par);
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
                            ParentId = input.Copy.Id,
                            Resource = item.Value,
                            Context = input.Context
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