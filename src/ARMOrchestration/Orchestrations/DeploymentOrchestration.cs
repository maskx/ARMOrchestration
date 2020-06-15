using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestration : TaskOrchestration<TaskResult, string>
    {
        public static string Name { get { return "DeploymentOrchestration"; } }
        private readonly ARMTemplateHelper helper;
        private readonly IInfrastructure infrastructure;
        private readonly ARMFunctions _ARMFunctions;

        public DeploymentOrchestration(
            ARMTemplateHelper helper,
            IInfrastructure infrastructure,
            ARMFunctions aRMFunctions)
        {
            this._ARMFunctions = aRMFunctions;
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

            #region validate template

            // when Template had value, this orchestration call by internal or processed by BeforeDeployment,the template string content already be parsed
            if (input.Template == null)
            {
                var valid = await context.ScheduleTask<TaskResult>(ValidateTemplateActivity.Name, "1.0", input);
                if (valid.Code != 200)
                {
                    return valid;
                }
                input = DataConverter.Deserialize<DeploymentOrchestrationInput>(valid.Content);
            }

            #endregion validate template

            #region InjectBeforeDeployment

            if (infrastructure.InjectBeforeDeployment)
            {
                var injectBeforeDeploymenteResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                             RequestOrchestration.Name,
                             "1.0",
                             new AsyncRequestActivityInput()
                             {
                                 InstanceId = context.OrchestrationInstance.InstanceId,
                                 ExecutionId = context.OrchestrationInstance.ExecutionId,
                                 ProvisioningStage = ProvisioningStage.InjectBeforeDeployment,
                                 DeploymentContext = input,
                                 Resource = null
                             });
                if (injectBeforeDeploymenteResult.Code != 200)
                {
                    return injectBeforeDeploymenteResult;
                }
            }

            #endregion InjectBeforeDeployment

            #region Before Deployment

            if (infrastructure.BeforeDeploymentOrchestration != null)
            {
                foreach (var t in infrastructure.BeforeDeploymentOrchestration)
                {
                    var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                    if (r.Code != 200)
                    {
                        helper.SaveDeploymentOperation(new DeploymentOperation(input, infrastructure, null)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.BeforeDeploymentFailed,
                            Result = DataConverter.Serialize(r)
                        });
                        return r;
                    }

                    input = DataConverter.Deserialize<DeploymentOrchestrationInput>(r.Content);
                }
            }

            #endregion Before Deployment

            #region DependsOn

            if (input.DependsOn.Count > 0)
            {
                waitHandler = new TaskCompletionSource<string>();
                await context.ScheduleTask<TaskResult>(WaitDependsOnActivity.Name, "1.0",
                    new WaitDependsOnActivityInput()
                    {
                        ProvisioningStage = ProvisioningStage.DependsOnWaited,
                        DeploymentContext = input,
                        Resource = null,
                        DependsOn = input.DependsOn
                    });
                await waitHandler.Task;
                var r = DataConverter.Deserialize<TaskResult>(waitHandler.Task.Result);
                if (r.Code != 200)
                {
                    helper.SaveDeploymentOperation(new DeploymentOperation(input, infrastructure, null)
                    {
                        InstanceId = context.OrchestrationInstance.InstanceId,
                        ExecutionId = context.OrchestrationInstance.ExecutionId,
                        Stage = ProvisioningStage.DependsOnWaitedFailed,
                        Result = DataConverter.Serialize(r)
                    });
                    return r;
                }
            }

            #endregion DependsOn

            bool hasFailResource = false;

            #region Provisioning resources

            List<Task<TaskResult>> tasks = new List<Task<TaskResult>>();

            foreach (var resource in input.Template.Resources)
            {
                if (resource.FullType == infrastructure.BuitinServiceTypes.Deployments)
                {
                    continue;
                }
                tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(
                    ResourceOrchestration.Name,
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
                   DeploymentOrchestration.Name,
                    "1.0",
                   DataConverter.Serialize(deploy.Value)));
            }
            await Task.WhenAll(tasks.ToArray());
            foreach (var t in tasks)
            {
                if (t.Result.Code != 200)
                {
                    hasFailResource = true;
                    break;
                }
            }

            #endregion Provisioning resources

            if (input.Mode == DeploymentMode.Complete)
            {
                // TODO: complete mode, delete resource not exist in template
            }
            string rtv = string.Empty;

            #region After Deployment

            if (infrastructure.AfterDeploymentOrhcestration != null)
            {
                foreach (var t in infrastructure.AfterDeploymentOrhcestration)
                {
                    var r = await context.CreateSubOrchestrationInstance<TaskResult>(t.Name, t.Version, input);
                    if (r.Code != 200)
                    {
                        helper.SaveDeploymentOperation(new DeploymentOperation(input, infrastructure, null)
                        {
                            InstanceId = context.OrchestrationInstance.InstanceId,
                            ExecutionId = context.OrchestrationInstance.ExecutionId,
                            Stage = ProvisioningStage.AfterDeploymentOrhcestrationFailed,
                            Result = DataConverter.Serialize(r)
                        });
                        return r;
                    }
                    input = DataConverter.Deserialize<DeploymentOrchestrationInput>(r.Content);
                }
            }

            #endregion After Deployment

            if (infrastructure.InjectAfterDeployment)
            {
                if (infrastructure.InjectBeforeDeployment)
                {
                    var injectAfterDeploymenteResult = await context.CreateSubOrchestrationInstance<TaskResult>(
                                 RequestOrchestration.Name,
                                 "1.0",
                                 new AsyncRequestActivityInput()
                                 {
                                     InstanceId = context.OrchestrationInstance.InstanceId,
                                     ExecutionId = context.OrchestrationInstance.ExecutionId,
                                     ProvisioningStage = ProvisioningStage.InjectAfterDeployment,
                                     DeploymentContext = input,
                                     Resource = null
                                 });
                    if (injectAfterDeploymenteResult.Code != 200)
                    {
                        return injectAfterDeploymenteResult;
                    }
                }
            }

            #region get template outputs

            if (!string.IsNullOrEmpty(input.Template.Outputs))
            {
                rtv = this.GetOutputs(input);
            }

            #endregion get template outputs

            helper.SaveDeploymentOperation(new DeploymentOperation(input, infrastructure, null)
            {
                InstanceId = context.OrchestrationInstance.InstanceId,
                ExecutionId = context.OrchestrationInstance.ExecutionId,
                Stage = hasFailResource ? ProvisioningStage.Failed : ProvisioningStage.Successed,
                Result = rtv
            });
            return new TaskResult() { Code = 200, Content = rtv };
        }

        private TaskCompletionSource<string> waitHandler = null;

        public override void OnEvent(OrchestrationContext context, string name, string input)
        {
            if (this.waitHandler != null && name == ProvisioningStage.DependsOnWaited.ToString() && this.waitHandler.Task.Status == TaskStatus.WaitingForActivation)
            {
                this.waitHandler.SetResult(input);
            }
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
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-outputs?tabs=azure-powershell#conditional-output
                if (item.Value.TryGetProperty("condition", out JsonElement condition))
                {
                    if (condition.ValueKind == JsonValueKind.False)
                        continue;
                    if (condition.ValueKind == JsonValueKind.String &&
                        !(bool)this._ARMFunctions.Evaluate(condition.GetString(), context))
                        continue;
                }
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