using DurableTask.Core;
using maskx.ARMOrchestration.Activities;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.OrchestrationService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class DeploymentOrchestration : TaskOrchestration<TaskResult, string>
    {
        public const string Name = "DeploymentOrchestration";
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

            if (string.IsNullOrEmpty(input.RootId))
            {
                input.RootId = input.DeploymentId;
            }

            #region validate template

            // when Template had value, this orchestration call by internal, the template string content already be parsed
            if (input.Template == null || input.Deployments.Count > 0)
            {
                var valid = await context.ScheduleTask<TaskResult>(ValidateTemplateActivity.Name, "1.0", input);
                if (valid.Code != 200)
                {
                    return valid;
                }
                input = DataConverter.Deserialize<DeploymentOrchestrationInput>(valid.Content);
            }
            else
            {
                helper.SaveDeploymentOperation(new DeploymentOperation(input, infrastructure)
                {
                    InstanceId = context.OrchestrationInstance.InstanceId,
                    ExecutionId = context.OrchestrationInstance.ExecutionId,
                    Input = arg
                }); ;
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
            ConcurrentBag<Task<TaskResult>> tasks = new ConcurrentBag<Task<TaskResult>>();

            Dictionary<string, List<Resource>> copyDic = new Dictionary<string, List<Resource>>();
            foreach (var resource in input.Template.Resources.Values)
            {
                if (resource.FullType == infrastructure.BuiltinServiceTypes.Deployments
                    || resource.Type == Copy.ServiceType)
                    continue;
                if (!string.IsNullOrEmpty(resource.CopyId))
                {
                    if (!copyDic.TryGetValue(resource.CopyName, out List<Resource> rList))
                    {
                        rList = new List<Resource>();
                        copyDic.Add(resource.CopyName, rList);
                    }
                    rList.Add(resource);
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
            foreach (var key in copyDic.Keys)
            {
                var c = input.Template.Resources[key] as CopyResource;
                tasks.Add(context.CreateSubOrchestrationInstance<TaskResult>(CopyOrchestration.Name, "1.0", new CopyOrchestrationInput()
                {
                    Resource = c,
                    Context = input,
                    Resources = copyDic[key]
                }));
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
            string rtv = null;

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

            #region get template outputs

            if (!string.IsNullOrEmpty(input.Template.Outputs))
            {
                try
                {
                    rtv = this.GetOutputs(input);
                }
                catch (Exception ex)
                {
                    rtv = ex.Message;
                }

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
            writer.WriteString("type", infrastructure.BuiltinServiceTypes.Deployments);

            #region properties

            writer.WritePropertyName("properties");
            writer.WriteStartObject();

            #region outputs

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
                writer.WriteProperty(item, context, helper.ARMfunctions, infrastructure);
            }
            writer.WriteEndObject();

            #endregion outputs

            writer.WriteEndObject();

            #endregion properties

            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}