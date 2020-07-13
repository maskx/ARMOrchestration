using DurableTask.Core;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using System;

namespace maskx.ARMOrchestration.Activities
{
    public class ExpandResourcePropertiesActivity : TaskActivity<ResourceOrchestrationInput, TaskResult>
    {
        public const string Name = "ExpandResourcePropertiesActivity";
        private readonly ARMFunctions functions;
        private readonly IInfrastructure infrastructure;
        private readonly ARMTemplateHelper helper;
        public ExpandResourcePropertiesActivity(ARMFunctions functions, IInfrastructure infrastructure, ARMTemplateHelper helper)
        {
            this.functions = functions;
            this.infrastructure = infrastructure;
            this.helper = helper;
        }
        protected override TaskResult Execute(TaskContext context, ResourceOrchestrationInput input)
        {
            try
            {
                input.Resource.Properties = input.Resource.ExpandProperties(input.Context, functions, infrastructure);
                helper.SaveDeploymentOperation(new DeploymentOperation(input.Context, infrastructure, input.Resource)
                {
                    InstanceId = context.OrchestrationInstance.InstanceId,
                    ExecutionId = context.OrchestrationInstance.ExecutionId,
                    Input = this.DataConverter.Serialize(input)
                });
            }
            catch (Exception ex)
            {
                return new TaskResult() { Code = 500, Content = ex.Message };
            }
            return new TaskResult() { Code = 200 ,Content=this.DataConverter.Serialize(input.Resource)};
        }
    }
}
