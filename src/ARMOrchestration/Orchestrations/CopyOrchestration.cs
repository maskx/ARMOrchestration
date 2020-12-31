﻿using DurableTask.Core;
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
            input.Deployment.IsRuntime = true;
            var copy = input.Resource.Copy;
            if (copy == null)
                return new TaskResult(500, new ErrorResponse() { Code = "CopyOrchestration-Fail", Message = "input is not Copy resource" });
            if (!context.IsReplaying)
            {
                if (input.IsRetry)
                {
                    var r = helper.PrepareRetry(input.DeploymentOperationId, context.OrchestrationInstance.InstanceId, context.OrchestrationInstance.ExecutionId, input.LastRunUserId, DataConverter.Serialize(input));
                    if (r == null)
                        return new TaskResult(400, $"cannot find DeploymentOperation with Id:{input.DeploymentOperationId}");
                    if (r.Value == ProvisioningStage.Successed)
                        return new TaskResult(200, "");
                    if (r.Value != ProvisioningStage.StartProvisioning)
                        return new TaskResult(400, $"Deployment[{input.DeploymentOperationId}] in stage of [{r.Value}], cannot retry");
                }
                else
                {
                    helper.SaveDeploymentOperation(new DeploymentOperation(input.DeploymentOperationId, input.Resource)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.StartProvisioning,
                        Input = DataConverter.Serialize(input),
                        LastRunUserId = input.LastRunUserId
                    });
                }               
            }
            List<Task<TaskResult>> tasks = new List<Task<TaskResult>>();
            List<ErrorResponse> errorResponses = new List<ErrorResponse>();
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/copy-resources#iteration-for-a-child-resource
            // You can't use a copy loop for a child resource.
            for (int i = 0; i < copy.Count; i++)
            {
                var r = input.Resource.Copy.GetResource(i);
                if (input.Resource.Type == infrastructure.BuiltinServiceTypes.Deployments)
                    tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                        DeploymentOrchestration<T>.Name,
                        "1.0", 
                        DataConverter.Serialize(new ResourceOrchestrationInput()
                        {
                            DeploymentOperationId=r.DeploymentOperationId,
                            DeploymentId = r.Input.DeploymentId,
                            NameWithServiceType = r.NameWithServiceType,
                            ServiceProvider = r.ServiceProvider,
                            CopyIndex = r.CopyIndex ?? -1,
                            IsRetry=input.IsRetry,
                            LastRunUserId=input.LastRunUserId
                        })));
                else
                    helper.ProvisioningResource<T>(r, tasks, context,input.IsRetry,input.LastRunUserId);

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