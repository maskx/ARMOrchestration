using maskx.ARMOrchestration.Functions;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using maskx.ARMOrchestration.Orchestrations;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances
    /// </summary>
    public class Copy : ChangeTracking
    {
        public const string ServiceType = "copy";
        public const string SerialMode = "serial";
        public const string ParallelMode = "parallel";

        public DeploymentOrchestrationInput DeploymentOrchestrationInput { get; set; }
        private Resource _Resource;
        private IInfrastructure _Infrastructure { get { return DeploymentOrchestrationInput.ServiceProvider.GetService<IInfrastructure>(); } }
        private ARMFunctions _ARMFunctions { get { return DeploymentOrchestrationInput.ServiceProvider.GetService<ARMFunctions>(); } }
        private Dictionary<string, object> _ParentContext;

        public string Id
        {
            get
            {
                Dictionary<string, object> cxt = new Dictionary<string, object>();
                foreach (var item in _ParentContext)
                {
                    cxt.Add(item.Key, item.Value);
                }
                return _ARMFunctions.ResourceId(_Resource.Input, new object[] {
                    $"{_Infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}",
                    _Resource.Input.DeploymentName,
                    Name });
            }
        }
        /// <summary>
        /// name-of-loop
        /// </summary>
        public string Name
        {
            get
            {
                if (!RootElement.TryGetProperty("name", out JsonElement name))
                    throw new Exception("cannot find name prooerty");
                return name.GetString();
            }
        }

        public string Type { get { return $"{_Infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}"; } }
        private int? _Count;
        /// <summary>
        /// number-of-iterations
        /// </summary>
        public int Count
        {
            get
            {
                if (!_Count.HasValue)
                {
                    if (!RootElement.TryGetProperty("count", out JsonElement count))
                        throw new Exception("not find count in copy node");
                    if (count.ValueKind == JsonValueKind.Number)
                        _Count = count.GetInt32();
                    else if (count.ValueKind == JsonValueKind.String)
                    {
                        Dictionary<string, object> cxt = new Dictionary<string, object>();
                        foreach (var item in _ParentContext)
                        {
                            cxt.Add(item.Key, item.Value);
                        }
                        _Count = (int)_ARMFunctions.Evaluate(count.GetString(), cxt);
                        // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#valid-uses-1
                        if (cxt.ContainsKey(ContextKeys.DEPENDSON))
                            throw new Exception("You can't use the reference function to set the value of the count property in a copy loop.");
                    }
                    else
                        throw new Exception("the value of count property should be Number in copy node");

                }
                return _Count.Value;
            }
        }

        /// <summary>
        /// "serial" <or> "parallel"
        /// </summary>
        public string Mode
        {
            get
            {
                if (RootElement.TryGetProperty("mode", out JsonElement mode))
                    return mode.GetString().ToLower();
                return ParallelMode;
            }
        }
        private int? _BatchSize;
        /// <summary>
        /// number-to-deploy-serially
        /// </summary>
        public int BatchSize
        {
            get
            {
                if (!_BatchSize.HasValue)
                {
                    if (!RootElement.TryGetProperty("batchSize", out JsonElement batchSize))
                        _BatchSize = 0;
                    else
                    {
                        if (batchSize.ValueKind == JsonValueKind.Number)
                            _BatchSize = batchSize.GetInt32();
                        else if (batchSize.ValueKind == JsonValueKind.String)
                        {
                            Dictionary<string, object> cxt = new Dictionary<string, object>();
                            foreach (var item in _ParentContext)
                            {
                                cxt.Add(item.Key, item.Value);
                            }
                            _BatchSize = (int)_ARMFunctions.Evaluate(batchSize.GetString(), cxt);
                        }

                    }
                }
                return _BatchSize.Value;

            }
        }

        /// <summary>
        /// values-for-the-property-or-variable
        /// </summary>
        public string Input
        {
            get
            {
                if (RootElement.TryGetProperty("input", out JsonElement input))
                {
                    return input.GetRawText();
                }
                return null;
            }
        }

        public Copy(string rawString, Dictionary<string, object> context, Resource resource) : this(rawString, context)
        {
            this._Resource = resource;
        }
        public Copy(string rawString, Dictionary<string, object> context)
        {
            this.DeploymentOrchestrationInput = context[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
            this.RawString = rawString;
            this._ParentContext = context;
        }
        public IEnumerable<Resource> EnumerateResource(bool flatChild = false)
        {
            for (int i = 0; i < this.Count; i++)
            {
                var parentContext = new Dictionary<string, object>();
                foreach (var item in _Resource.FullContext)
                {
                    if (item.Key == ContextKeys.ARM_CONTEXT) continue;
                    parentContext.Add(item.Key, item.Value);
                }
                var r = new Resource()
                {
                    RawString = _Resource.RawString,
                    CopyIndex = i,
                    ParentContext = parentContext,
                    Input = _Resource.Input
                };
                yield return r;
                if (flatChild)
                {
                    foreach (var child in r.FlatEnumerateChild())
                    {
                        yield return child;
                    }
                }
            }
        }
    }
}