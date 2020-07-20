using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// 部署服务需提供以下API
    /// https://docs.microsoft.com/en-us/rest/api/resources/
    /// </summary>
    public class Resource
    {
        public bool Condition { get; set; } = true;

        public string ApiVersion { get; set; }

        /// <summary>
        /// the type set in template
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// the type of resource
        /// the child resource is parentType/childType
        /// </summary>
        public string FullType { get; set; }

        /// <summary>
        /// the name set in template
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// the name of resource
        /// the child resource is parentName/childName
        /// </summary>
        public string FullName { get; set; }

        public string Location { get; set; }

        public string Comments { get; set; }
        public List<string> Resources { get; set; } = new List<string>();

        /// <summary>
        /// The list can include resources that are conditionally deployed. When a conditional resource isn't deployed, Azure Resource Manager automatically removes it from the required dependencies.
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/define-resource-dependency#dependson
        ///
        /// Depend on resources in a loop
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances#depend-on-resources-in-a-loop
        ///
        ///
        /// You only need to define dependencies for resources that are deployed in the same template.
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/define-resource-dependency
        ///
        /// </summary>
        public List<string> DependsOn { get; set; } = new List<string>();

        public string Properties { get; set; }

        public SKU SKU { get; set; }

        public string Kind { get; set; }

        public string Plan { get; set; }
        public List<string> Zones { get; set; } = new List<string>();
        public string ResourceGroup { get; set; }

        public string SubscriptionId { get; set; }
        public string ManagementGroupId { get; set; }

        public string ResouceId { get; set; }
        public string CopyId { get; set; }
        public int CopyIndex { get; set; }
        public string CopyName { get; set; }

        public override string ToString()
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteBoolean("condition", this.Condition);
            writer.WriteString("apiVersion", this.ApiVersion);
            writer.WriteString("type", this.FullType);
            writer.WriteString("name", this.FullName);
            if (!string.IsNullOrEmpty(this.Location))
                writer.WriteString("location", this.Location);
            if (!string.IsNullOrEmpty(this.Comments))
                writer.WriteString("comments", this.Comments);
            if (this.DependsOn.Count > 0)
            {
                writer.WritePropertyName("dependsOn");
                writer.WriteStartArray();
                foreach (var depend in this.DependsOn)
                {
                    writer.WriteStringValue(depend);
                }
                writer.WriteEndArray();
            }

            if (!string.IsNullOrEmpty(this.Properties))
                writer.WriteRawString("properties", this.Properties);
            if (this.SKU != null)
                writer.WriteRawString("sku", this.SKU.ToString());
            if (!string.IsNullOrEmpty(this.Kind))
                writer.WriteString("kind", this.Kind);
            if (!string.IsNullOrEmpty(this.Plan))
                writer.WriteRawString("plan", this.Plan);
            if (!string.IsNullOrEmpty(this.ResourceGroup))
                writer.WriteString("resourceGroup", this.ResourceGroup);
            if (!string.IsNullOrEmpty(this.SubscriptionId))
                writer.WriteString("subscriptionId", this.SubscriptionId);
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static List<Resource> Parse(
            string rawString
            , Dictionary<string, object> context
            , ARMFunctions functions
            , IInfrastructure infrastructure
            , string parentName,
            string parentType)
        {
            using var doc = JsonDocument.Parse(rawString);
            return Parse(doc.RootElement, context, functions, infrastructure, parentName, parentType);

        }
        private static List<Resource> ParseInternal(
            JsonElement resourceElement
            , Dictionary<string, object> context
            , ARMFunctions functions
            , IInfrastructure infrastructure
            , string parentName,
            string parentType)
        {
            DeploymentContext deploymentContext = context[ContextKeys.ARM_CONTEXT] as DeploymentContext;
            List<Resource> resources = new List<Resource>();
            Resource r = new Resource();
            resources.Add(r);

            if (resourceElement.TryGetProperty("condition", out JsonElement condition))
            {
                if (condition.ValueKind == JsonValueKind.False)
                    r.Condition = false;
                else if (condition.ValueKind == JsonValueKind.String)
                    r.Condition = (bool)functions.Evaluate(condition.GetString(), context);
            }

            if (resourceElement.TryGetProperty("apiVersion", out JsonElement apiVersion))
                r.ApiVersion = apiVersion.GetString();
            else
                throw new Exception("not find apiVersion in resource node");

            if (resourceElement.TryGetProperty("type", out JsonElement type))
                r.Type = type.GetString();
            else
                throw new Exception("not find type in resource node");

            if (!string.IsNullOrEmpty(parentType))
                r.FullType = $"{parentType}/{r.Type}";
            else
                r.FullType = r.Type;

            if (resourceElement.TryGetProperty("name", out JsonElement name))
                r.Name = functions.Evaluate(name.GetString(), context).ToString();
            else
                throw new Exception("not find name in resource node");

            if (!string.IsNullOrEmpty(parentName))
                r.FullName = $"{parentName}/{r.Name}";
            else
                r.FullName = r.Name;

            if (resourceElement.TryGetProperty("resourceGroup", out JsonElement resourceGroup))
                r.ResourceGroup = functions.Evaluate(resourceGroup.GetString(), context).ToString();
            else
                r.ResourceGroup = deploymentContext.ResourceGroup;

            if (resourceElement.TryGetProperty("subscriptionId", out JsonElement subscriptionId))
                r.SubscriptionId = functions.Evaluate(subscriptionId.GetString(), context).ToString();
            else
                r.SubscriptionId = deploymentContext.SubscriptionId;

            if (resourceElement.TryGetProperty("managementGroupId", out JsonElement managementGroupId))
                r.ManagementGroupId = functions.Evaluate(managementGroupId.GetString(), context).ToString();
            else
                r.ManagementGroupId = deploymentContext.ManagementGroupId;

            if (resourceElement.TryGetProperty("location", out JsonElement location))
                r.Location = functions.Evaluate(location.GetString(), context).ToString();

            if (resourceElement.TryGetProperty("comments", out JsonElement comments))
                r.Comments = comments.GetString();

            if (resourceElement.TryGetProperty("dependsOn", out JsonElement dependsOn))
            {
                using var dd = JsonDocument.Parse(dependsOn.GetRawText());
                foreach (var item in dd.RootElement.EnumerateArray())
                {
                    r.DependsOn.Add(functions.Evaluate(item.GetString(), context).ToString());
                }
            }

            #region ResouceId

            if (deploymentContext.Template.DeployLevel == DeployLevel.ResourceGroup)
            {
                List<object> pars = new List<object>
                {
                    r.SubscriptionId,
                    r.ResourceGroup,
                    r.FullType
                };
                pars.AddRange(r.FullName.Split('/'));
                r.ResouceId = functions.ResourceId(
                   deploymentContext,
                   pars.ToArray());
            }
            else if (deploymentContext.Template.DeployLevel == DeployLevel.Subscription)
            {
                List<object> pars = new List<object>
                {
                    r.SubscriptionId,
                    r.FullType
                };
                pars.AddRange(r.FullName.Split('/'));
                r.ResouceId = functions.SubscriptionResourceId(deploymentContext, pars.ToArray());
            }
            else
            {
                List<object> pars = new List<object>
                {
                    r.FullType
                };
                pars.AddRange(r.FullName.Split('/'));
                r.ResouceId = functions.TenantResourceId(pars.ToArray());
            }

            #endregion ResouceId

            if (resourceElement.TryGetProperty("sku", out JsonElement sku))
                r.SKU = SKU.Parse(sku, functions, context);
            else
                r.SKU = new SKU() { Name = SKU.Default };

            if (resourceElement.TryGetProperty("kind", out JsonElement kind))
                r.Kind = kind.GetString();

            if (resourceElement.TryGetProperty("plan", out JsonElement plan))
                r.Plan = plan.GetRawText();

            if (resourceElement.TryGetProperty("zones", out JsonElement zones))
            {
                foreach (var z in zones.EnumerateArray())
                {
                    r.Zones.Add(functions.Evaluate(z.GetString(), context).ToString());
                }
            }

            if (context.ContainsKey(ContextKeys.DEPENDSON))
            {
                //https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#valid-uses-1
                throw new Exception("The reference function can only be used in the properties of a resource definition and the outputs section of a template or deployment.");
            }

            if (!r.Condition)
                return resources;

            if (resourceElement.TryGetProperty("properties", out JsonElement properties))
            {
                if (r.FullType == infrastructure.BuiltinServiceTypes.Deployments)
                    r.Properties = properties.GetRawText();
                else
                {
                    r.Properties = properties.ExpandObject(context, functions, infrastructure);
                    // if there has Implicit dependency by reference function in properties
                    // the reference function should be evaluate at provisioning time
                    // so keep the original text
                    if (HandleDependsOn(r, context))
                        r.Properties = properties.GetRawText();
                }
            }
            if (resourceElement.TryGetProperty("resources", out JsonElement _resources))
            {
                foreach (var childres in _resources.EnumerateArray())
                {
                    //https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/child-resource-name-type
                    var childResult = Resource.Parse(childres.GetRawText(), context, functions, infrastructure, r.Name, r.Type);
                    r.Resources.Add(childResult[0].Name);
                    resources.AddRange(childResult);
                }
            }
            return resources;
        }
        public static List<Resource> Parse(
            JsonElement resourceElement
            , Dictionary<string, object> context
            , ARMFunctions functions
            , IInfrastructure infrastructure
            , string parentName,
            string parentType)
        {
            if (resourceElement.TryGetProperty("copy", out JsonElement copy))
                return ExpandCopyResource(resourceElement, copy, context, functions, infrastructure, parentName, parentType);
            return ParseInternal(resourceElement, context, functions, infrastructure, parentName, parentType);
        }
        private static List<Resource> ExpandCopyResource(
          JsonElement resource
          , JsonElement copyElement
          , Dictionary<string, object> context
            , ARMFunctions functions
            , IInfrastructure infrastructure
            , string parentName
            , string parentType)
        {
            Copy copy = Copy.Parse(copyElement, context, functions, infrastructure);

            DeploymentContext deploymentContext = context[ContextKeys.ARM_CONTEXT] as DeploymentContext;

            var CopyResource = new CopyResource()
            {
                Name = copy.Name,
                Type = Copy.ServiceType,
                FullName = $"{deploymentContext.DeploymentName}/{copy.Name}",
                FullType = $"{infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}",
                ResouceId = copy.Id,
                Mode=copy.Mode,
                BatchSize = copy.BatchSize,
            };
            List<Resource> resources = new List<Resource>
            {
                CopyResource
            };

            var copyindex = new Dictionary<string, int>() { { copy.Name, 0 } };
            Dictionary<string, object> copyContext = new Dictionary<string, object>
            {
                { ContextKeys.ARM_CONTEXT, deploymentContext },
                { ContextKeys.COPY_INDEX, copyindex },
                { ContextKeys.CURRENT_LOOP_NAME, copy.Name },
                { ContextKeys.IS_PREPARE, true }
            };

            for (int i = 0; i < copy.Count; i++)
            {
                copyindex[copy.Name] = i;
                var rs = ParseInternal(resource, copyContext, functions, infrastructure, parentName, parentType);

                rs[0].CopyIndex = i;
                rs[0].CopyId = copy.Id;
                rs[0].CopyName = copy.Name;
                CopyResource.Resources.Add(rs[0].Name);
                resources.AddRange(rs);
            }
            CopyResource.SubscriptionId = resources[1].SubscriptionId;
            CopyResource.ManagementGroupId = resources[1].ManagementGroupId;
            CopyResource.SKU = resources[1].SKU;
            CopyResource.Plan = resources[1].Plan;
            CopyResource.Kind = resources[1].Kind;
            CopyResource.Zones = resources[1].Zones;
            CopyResource.Location = resources[1].Location;
            return resources;
        }
        private static bool HandleDependsOn(Resource r, Dictionary<string, object> context)
        {
            if (context.TryGetValue(ContextKeys.DEPENDSON, out object conditionDep))
            {
                r.DependsOn.AddRange(conditionDep as List<string>);
                context.Remove(ContextKeys.DEPENDSON);
            }
            if (context.ContainsKey(ContextKeys.NEED_REEVALUATE))
            {
                context.Remove(ContextKeys.NEED_REEVALUATE);
                return true;
            }
            return false;
        }

        public string ExpandProperties(DeploymentContext deploymentContext, ARMFunctions functions, IInfrastructure infrastructure)
        {
            if (string.IsNullOrEmpty(this.Properties))
                return string.Empty;
            {
                var doc = JsonDocument.Parse(this.Properties);
                Dictionary<string, object> cxt = new Dictionary<string, object>() { { ContextKeys.ARM_CONTEXT, deploymentContext } };
                if (!string.IsNullOrEmpty(this.CopyName))
                {
                    cxt.Add(ContextKeys.CURRENT_LOOP_NAME, this.CopyName);
                    cxt.Add(ContextKeys.COPY_INDEX, new Dictionary<string, int>() { { this.CopyName, this.CopyIndex } });
                }
                return doc.RootElement.ExpandObject(cxt, functions, infrastructure);
            }
        }
    }
}