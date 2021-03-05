using maskx.ARMOrchestration.Functions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.ARMTemplate
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances
    /// </summary>
    public class Copy : ObjectChangeTracking
    {
        public const string ServiceType = "copy";
        public const string SerialMode = "serial";
        public const string ParallelMode = "parallel";

        internal Resource Resource;

        public string Id
        {
            get
            {
                Dictionary<string, object> cxt = new Dictionary<string, object>();
                foreach (var item in _ParentContext)
                {
                    cxt.Add(item.Key, item.Value);
                }
                return ARMFunctions.ResourceId(Resource.Deployment, new object[] {
                    $"{Infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}",
                    Resource.Deployment.Name,
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
                if (!RootElement.TryGetValue("name", out JToken name))
                    throw new Exception("cannot find name prooerty");
                return name.Value<string>();
            }
            set
            {
                RootElement["name"] = value;
            }
        }
        public string Type
        {
            get { return Copy.ServiceType; }
        }
        public string FullType { get { return $"{Infrastructure.BuiltinServiceTypes.Deployments}/{Copy.ServiceType}"; } }
        public string FullName
        {
            get
            {

                return $"{Resource.Deployment.Name}/{this.Name}";
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
                    if (!RootElement.TryGetValue("count", out JToken count))
                        throw new Exception("not find count in copy node");
                    if (count.Type == JTokenType.Integer)
                        _Count = count.Value<int>();
                    else if (count.Type == JTokenType.String)
                    {
                        Dictionary<string, object> cxt = new Dictionary<string, object>();
                        foreach (var item in _ParentContext)
                        {
                            cxt.Add(item.Key, item.Value);
                        }
                        _Count = (int)ARMFunctions.Evaluate(count.Value<string>(), cxt, $"{RootElement.Path}.count");
                        // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#valid-uses-1
                        if (cxt.ContainsKey(ContextKeys.DEPENDSON))
                            throw new Exception("You can't use the reference function to set the value of the count property in a copy loop.");
                    }
                    else
                        throw new Exception("the value of count property should be Number in copy node");

                }
                return _Count.Value;
            }
            set
            {
                _Count = value;
                RootElement["count"] = value;
            }
        }

        /// <summary>
        /// "serial" <or> "parallel"
        /// </summary>
        public string Mode
        {
            get
            {
                if (RootElement.TryGetValue("mode", out JToken mode))
                    return mode.Value<string>().ToLower();
                return ParallelMode;
            }
            set
            {
                RootElement["mode"] = value;
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
                    if (!RootElement.TryGetValue("batchSize", out JToken batchSize))
                        _BatchSize = 0;
                    else
                    {
                        if (batchSize.Type == JTokenType.Integer)
                            _BatchSize = batchSize.Value<Int32>();
                        else if (batchSize.Type == JTokenType.String)
                        {
                            Dictionary<string, object> cxt = new Dictionary<string, object>();
                            foreach (var item in _ParentContext)
                            {
                                cxt.Add(item.Key, item.Value);
                            }
                            _BatchSize = (int)ARMFunctions.Evaluate(batchSize.Value<string>(), cxt, $"{RootElement.Path}.batchSize");
                        }

                    }
                }
                return _BatchSize.Value;
            }
            set
            {
                _BatchSize = value;
                RootElement["batchSize"] = value;
            }
        }

        /// <summary>
        /// values-for-the-property-or-variable
        /// </summary>
        public string Input
        {
            get
            {
                if (RootElement.TryGetValue("input", out JToken input))
                {
                    return input.ToString();
                }
                return null;
            }
            set
            {
                RootElement["input"] = value;
            }
        }
        public Copy() : base(new JObject(), null) { }
        public Copy(JObject root, Dictionary<string, object> context, Resource resource) : this(root, context)
        {
            this.Resource = resource;
        }
        public Copy(JObject root, Dictionary<string, object> context) : base(root, context)
        {
        }
        public IEnumerable<Resource> EnumerateResource(bool flatChild = false)
        {
            for (int i = 0; i < this.Count; i++)
            {
                var r = GetResource(i);
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
            return new Resource(Resource.RootElement, Resource.FullContext, index);
        }
    }
}