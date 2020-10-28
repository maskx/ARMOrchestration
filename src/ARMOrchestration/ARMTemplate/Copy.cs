using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text.Json;

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

        public Deployment DeploymentOrchestrationInput { get; set; }
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
                    _Resource.Input.Name,
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
        public string Type
        {
            get { return Copy.ServiceType; }
        }
        public string FullType { get { return $"{_Infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}"; } }
        public string FullName
        {
            get
            {

                return $"{DeploymentOrchestrationInput.Name}/{this.Name}";
            }
        }
        public string NameWithServiceType
        {
            get
            {
                var ns = FullName.Split('/');
                var ts = FullType.Split('/');
                string nestr = string.Empty;
                for (int i = 1; i < ns.Length; i++)
                {
                    nestr += $"/{ts[i + 1]}/{ns[i]}";
                }
                return $"{ts[0]}/{ts[1]}/{ns[0]}{nestr}";
            }
        }
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
            this.DeploymentOrchestrationInput = context[ContextKeys.ARM_CONTEXT] as Deployment;
            this.RawString = rawString;
            this._ParentContext = context;
        }
        public IEnumerable<Resource> EnumerateResource(bool flatChild = false)
        {
            for (int i = 0; i < this.Count; i++)
            {
                var r = GetResource(i);
                if (_Resource.Input.Template.ChangedCopyResoures.TryGetValue(r.NameWithServiceType, out Resource cr))
                    yield return cr;
                else
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
        public Resource GetResource(int index)
        {
            return new Resource(_Resource.RawString, _Resource.FullContext)
            {
                CopyIndex = index
            };
        }
    }
}