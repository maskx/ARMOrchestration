using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class CopyOrchestration<T> : TaskOrchestration<TaskResult, ResourceOrchestrationInput>
        where T : CommunicationJob, new()
    {
        public const string Name = "CopyOrchestration";
        private readonly ARMTemplateHelper helper;
        private readonly IServiceProvider _ServiceProvider;

        public CopyOrchestration(ARMTemplateHelper helper, IServiceProvider service)
        {
            this._ServiceProvider = service;
            this.helper = helper;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, ResourceOrchestrationInput input)
        {
            input.ServiceProvider = _ServiceProvider;
            if (!input.IsRetry)
                input.DeploymentOperationId = context.OrchestrationInstance.InstanceId;
            var copy = input.Resource.Copy;
            if (copy == null)
                return new TaskResult(500, new ErrorResponse() { Code = "CopyOrchestration-Fail", Message = "input is not Copy resource" });
            if (!context.IsReplaying)
            {
                if (input.IsRetry)
                {
                    var r = helper.PrepareRetry(input.DeploymentOperationId, context.OrchestrationInstance.InstanceId, context.OrchestrationInstance.ExecutionId, input.LastRunUserId, DataConverter.Serialize(input));
                    if (r == null)
                        return new TaskResult(400, new ErrorResponse()
                        {
                            Code = $"{Name}:PrepareRetry",
                            Message = $"cannot find DeploymentOperation with Id:{input.DeploymentOperationId}"
                        });
                    if (r.Value == ProvisioningStage.Successed)
                        return new TaskResult(200, "");
                    if (r.Value != ProvisioningStage.StartProvisioning)
                        return new TaskResult(400, new ErrorResponse()
                        {
                            Code = $"{Name}:PrepareRetry",
                            Message = $"Deployment[{input.DeploymentOperationId}] in stage of [{r.Value}], only failed deployment support retry"
                        });
                }
                else
                {
                    var operation = helper.CreatDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId, input.Resource)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.StartProvisioning,
                        Input = input.Resource.ToString(),
                        LastRunUserId = input.LastRunUserId
                    }).Result;
                    if (operation == null)
                        return new TaskResult(400, new ErrorResponse()
                        {
                            Code = $"{Name}:CreatDeploymentOperation",
                            Message = "CorrelationId duplicated"
                        });
                    if (operation.Id != input.DeploymentOperationId)
                        return new TaskResult(400, new ErrorResponse()
                        {
                            Code = $"{Name}:CreatDeploymentOperation",
                            Message = $"{operation.ResourceId} already exists"
                        });
                }
            }
            List<Task<TaskResult>> tasks = new List<Task<TaskResult>>();
            List<ErrorResponse> errorResponses = new List<ErrorResponse>();
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/copy-resources#iteration-for-a-child-resource
            // You can't use a copy loop for a child resource.
            for (int i = 0; i < copy.Count; i++)
            {
                helper.ProvisioningResource<T>(input.Resource.Copy.GetResource(i), tasks, context, input.IsRetry, input.LastRunUserId);
                if (copy.BatchSize > 0 && tasks.Count >= copy.BatchSize)
                {
                    await Task.WhenAny(tasks);

                    List<Task<TaskResult>> temp = new List<Task<TaskResult>>();
                    foreach (var item in tasks)
                    {
                        if (item.IsCompleted)
                            helper.ParseTaskResult(Name, errorResponses, item);
                        else
                            temp.Add(item);
                    }
                    tasks = temp;
                }
            }
            await Task.WhenAll(tasks);
            foreach (var item in tasks) helper.ParseTaskResult(Name, errorResponses, item);
            if (errorResponses.Count > 0)
            {
                helper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                {
                    Stage = ProvisioningStage.Failed,
                    Result = DataConverter.Serialize(errorResponses)
                });
                return new TaskResult(500, errorResponses);
            }
            else
            {
                helper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId)
                {
                    Stage = ProvisioningStage.Successed
                });
                return new TaskResult() { Code = 200 };
            }
        }
    }
}