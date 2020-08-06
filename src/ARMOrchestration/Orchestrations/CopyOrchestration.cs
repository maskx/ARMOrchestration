﻿using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
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

            for (int i = 0; i < copy.Count; i++)
            {
                tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                                   ResourceOrchestration.Name,
                                   "1.0",
                                   new ResourceOrchestrationInput()
                                   {
                                       Resource = new Resource()
                                       {
                                           RawString = input.Resource.RawString,
                                           CopyIndex = i
                                       },
                                       Context = input.Context,
                                   }));
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
                helper.SaveDeploymentOperation(new DeploymentOperation(input.Context, infrastructure, input.Resource)
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
                helper.SaveDeploymentOperation(new DeploymentOperation(input.Context, infrastructure, input.Resource)
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