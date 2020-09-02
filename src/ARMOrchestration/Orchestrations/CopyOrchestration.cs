using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class CopyOrchestration : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
    {
        public const string Name = "CopyOrchestration";
        private readonly ARMTemplateHelper helper;
        private readonly IInfrastructure infrastructure;
        private readonly IServiceProvider _ServiceProvider;

        public CopyOrchestration(ARMTemplateHelper helper, IInfrastructure infrastructure, IServiceProvider service)
        {
            this._ServiceProvider = service;
            this.helper = helper;
            this.infrastructure = infrastructure;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            input.ServiceProvider = _ServiceProvider;
            var copy = input.Resource.Copy;
            ConcurrentBag<string> msg = new ConcurrentBag<string>();
            List<Task<TaskResult>> tasks = new List<Task<TaskResult>>();

            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/copy-resources#iteration-for-a-child-resource
            // You can't use a copy loop for a child resource.
            for (int i = 0; i < copy.Count; i++)
            {
                var ParentContext = new Dictionary<string, object>();
                foreach (var item in input.Resource.FullContext)
                {
                    if (item.Key == ContextKeys.ARM_CONTEXT) continue;
                    ParentContext.Add(item.Key, item.Value);
                }

                if (input.Resource.Type == infrastructure.BuiltinServiceTypes.Deployments)
                {
                    var deploy = DeploymentOrchestrationInput.Parse(new Resource()
                    {
                        RawString = input.Resource.RawString,
                        CopyIndex = i,
                        ParentContext = ParentContext,
                        Input = input.Input
                    });
                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                        DeploymentOrchestration.Name,
                        "1.0",
                        DataConverter.Serialize(deploy)));
                }
                else
                {
                    helper.ProvisioningResource(new Resource()
                    {
                        RawString = input.Resource.RawString,
                        CopyIndex = i,
                        ParentContext = ParentContext,
                        Input = input.Input
                    }, tasks, context, input.Input);
                }

                if (copy.BatchSize > 0 && tasks.Count >= copy.BatchSize)
                {
                    await Task.WhenAny(tasks);

                    List<Task<TaskResult>> temp = new List<Task<TaskResult>>();
                    foreach (var item in tasks)
                    {
                        if (item.IsCompleted)
                        {
                            if (item.Result.Code != 200)
                                msg.Add(item.Result.Content);
                        }
                        else
                            temp.Add(item);
                    }
                    tasks = temp;
                }
            }
            await Task.WhenAll(tasks);
            foreach (var item in tasks)
            {
                if (item.Result.Code != 200)
                    msg.Add(item.Result.Content);
            }
            if (msg.Count > 0)
            {
                helper.SaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                {
                    InstanceId = context.OrchestrationInstance.InstanceId,
                    ExecutionId = context.OrchestrationInstance.ExecutionId,
                    Stage = ProvisioningStage.Failed,
                    Input = DataConverter.Serialize(input),
                    Result = string.Join(Environment.NewLine, msg.ToArray())
                });
                return new TaskResult() { Code = 500, Content = string.Join(Environment.NewLine, msg.ToArray()) };
            }
            else
            {
                helper.SaveDeploymentOperation(new DeploymentOperation(input.Input, input.Resource)
                {
                    InstanceId = context.OrchestrationInstance.InstanceId,
                    ExecutionId = context.OrchestrationInstance.ExecutionId,
                    Stage = ProvisioningStage.Successed,
                    Input = DataConverter.Serialize(input)
                });
                return new TaskResult() { Code = 200 };
            }
        }
    }
}