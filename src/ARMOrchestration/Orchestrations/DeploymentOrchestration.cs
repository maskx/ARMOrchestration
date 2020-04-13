using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestration : TaskOrchestration<TaskResult, string>
    {
        private readonly ARMOrchestrationOptions ARMOptions;
        private readonly IServiceProvider serviceProvider;
        private readonly ARMFunctions functions;
        private readonly ARMTemplateHelper helper;
        private readonly IInfrastructure infrastructure;

        public DeploymentOrchestration(
            IOptions<ARMOrchestrationOptions> options,
            IServiceProvider serviceProvider,
            ARMFunctions functions,
            ARMTemplateHelper helper,
            IInfrastructure infrastructure)
        {
            this.ARMOptions = options?.Value;
            this.serviceProvider = serviceProvider;
            this.functions = functions;
            this.helper = helper;
            this.infrastructure = infrastructure;
        }

        public override async Task<TaskResult> RunTask(OrchestrationContext context, string arg)
        {
            DeploymentOrchestrationInput input = this.DataConverter.Deserialize<DeploymentOrchestrationInput>(arg);
            if (string.IsNullOrEmpty(input.DeploymentId))
            {
                input.DeploymentId = context.OrchestrationInstance.InstanceId;
            }
            if (string.IsNullOrEmpty(input.RootId))
            {
                input.RootId = input.DeploymentId;
                input.ParentId = $"{context.OrchestrationInstance.InstanceId}:{ context.OrchestrationInstance.ExecutionId}";
            }

            var operationArgs = new DeploymentOperationActivityInput()
            {
                DeploymentContext = input,
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Name = input.DeploymentName,
                Type = infrastructure.BuitinServiceTypes.Deployments,
                ParentId = input.ParentId,
                Stage = ProvisioningStage.StartProcessing,
                Input = DataConverter.Serialize(input)
            };
            if (!string.IsNullOrEmpty(input.SubscriptionId))
                operationArgs.ResourceId = $"/subscription/{input.SubscriptionId}";
            else if (!string.IsNullOrEmpty(input.ManagementGroupId))
                operationArgs.ResourceId = $"/managementgroup/{input.ManagementGroupId}";
            if (!string.IsNullOrEmpty(input.ResourceGroup))
                operationArgs.ResourceId += $"/resourceGroups/{input.ResourceGroup}";
            operationArgs.ResourceId += $"/providers/{infrastructure.BuitinServiceTypes.Deployments}/{input.DeploymentName}";

            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);

            #region DependsOn

            if (input.DependsOn.Count > 0)
            {
                operationArgs.Stage = ProvisioningStage.DependsOnWaited;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
                await context.CreateSubOrchestrationInstance<TaskResult>(
                    typeof(WaitDependsOnOrchestration).Name,
                    "1.0",
                    (input.ParentId, input.DependsOn));
                operationArgs.Stage = ProvisioningStage.DependsOnSuccessed;
                await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);
            }

            #endregion DependsOn

            string rtv = string.Empty;

            #region validate template

            var valid = await context.ScheduleTask<TaskResult>(typeof(ValidateTemplateActivity).Name, "1.0", input);
            if (valid.Code != 200)
                return valid;

            #endregion validate template

            input = DataConverter.Deserialize<DeploymentOrchestrationInput>(valid.Content);

            Dictionary<string, object> armContext = new Dictionary<string, object>() {
                { "armcontext", input as DeploymentContext}
            };

            // TODO: add BeforeDeployment

            #region Provisioning resources

            List<Task> tasks = new List<Task>();

            foreach (var resource in input.Template.Resources)
            {
                if (resource.FullType == infrastructure.BuitinServiceTypes.Deployments)
                {
                    continue;
                }
                tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                    typeof(ResourceOrchestration).Name,
                    "1.0",
                    new ResourceOrchestrationInput()
                    {
                        Resource = resource,
                        Context = input,
                    }));
            }
            foreach (var deploy in input.Deployments)
            {
                tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                    typeof(DeploymentOrchestration).Name,
                    "1.0",
                   DataConverter.Serialize(deploy.Value)));
            }
            await Task.WhenAll(tasks.ToArray());

            #endregion Provisioning resources

            if (input.Mode == DeploymentMode.Complete)
            {
                // TODO: complete mode, delete resource not exist in template
            }

            #region get template outputs

            if (!string.IsNullOrEmpty(input.Template.Outputs))
            {
                rtv = this.GetOutputs(input);
            }

            #endregion get template outputs

            operationArgs.Result = rtv;
            operationArgs.Stage = ProvisioningStage.Successed;
            await context.ScheduleTask<TaskResult>(typeof(DeploymentOperationActivity).Name, "1.0", operationArgs);

            return new TaskResult() { Code = 200, Content = rtv };
        }

        private string GetOutputs(DeploymentContext deploymentContext)
        {
            // https://docs.microsoft.com/en-us/rest/api/resources/deployments/get#deploymentextended
            string outputs = deploymentContext.Template.Outputs;
            Dictionary<string, object> context = new Dictionary<string, object>() {
                {"armcontext",deploymentContext }
            };
            using JsonDocument outDoc = JsonDocument.Parse(outputs);
            var outputDefineElement = outDoc.RootElement;
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms, new JsonWriterOptions() { Indented = false });
            writer.WriteStartObject();
            writer.WriteString("id", deploymentContext.DeploymentId);
            // TODO: set location
            writer.WriteString("location", deploymentContext.ResourceGroup);
            writer.WriteString("name", deploymentContext.DeploymentName);
            // TODO: set type
            writer.WriteString("type", deploymentContext.Mode.ToString());
            writer.WritePropertyName("properties");
            writer.WriteStartObject();
            writer.WritePropertyName("outputs");
            writer.WriteStartObject();
            foreach (var item in outputDefineElement.EnumerateObject())
            {
                writer.WritePropertyName(item.Name);
                writer.WriteStartObject();
                writer.WriteString("type", item.Value.GetProperty("type").GetString());
                writer.WritePropertyName("value");
                writer.WriteElement(item.Value.GetProperty("value"), context, helper);
                writer.WriteEndObject();
            }
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}