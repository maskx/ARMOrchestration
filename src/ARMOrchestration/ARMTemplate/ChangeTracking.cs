using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.ARMTemplate
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ChangeTracking
    {
        public Deployment Deployment { get { return _ParentContext[ContextKeys.ARM_CONTEXT] as Deployment; } }
        public IServiceProvider ServiceProvider { get { return Deployment.ServiceProvider; } }
        public ARMFunctions ARMFunctions { get { return ServiceProvider.GetService<ARMFunctions>(); } }
        public IInfrastructure Infrastructure { get { return ServiceProvider.GetService<IInfrastructure>(); } }
        internal Dictionary<string, object> _ParentContext;
        internal virtual Dictionary<string, object> FullContext
        {
            get
            {
                return _ParentContext.CopyNew();
            }
        }
        [JsonProperty]
        public string JsonPath
        {
            get { return Root?.Path; }
        }
        public ChangeTracking()
        {

        }
        public ChangeTracking(JToken root, Dictionary<string, object> parentContext)
        {
            this._ParentContext = parentContext;
            this.Root = root;
        }

        internal JToken Root
        {
            get; set;
        }
        public override string ToString()
        {
            return Root?.ToString(Formatting.Indented);
        }
    }
    public class ObjectChangeTracking : ChangeTracking
    {
        public JObject RootElement { get { return Root as JObject; } }
        public ObjectChangeTracking()
        {

        }
        public ObjectChangeTracking(JObject root, Dictionary<string, object> context) : base(root, context)
        {

        }
        public T GetValue<T>(string name)
        {
            if (RootElement.TryGetValue(name, out JToken e))
            {
                object o = null;
                if (e.Type == JTokenType.String)
                    o = this.ARMFunctions.Evaluate(e.Value<string>(), FullContext, $"{RootElement.Path}.{name}");
                else if (e.Type == JTokenType.Boolean)
                    o = e.Value<bool>();
                else if (e.Type == JTokenType.Integer)
                    o = e.Value<int>();
                return (T)o;
            }
            else
                return default;
        }
        public override string ToString()
        {
            return Root == null ? "{}" : Root.ToString(Formatting.Indented);
        }
    }
    public class ArrayChangeTracking : ChangeTracking
    {
        public JArray RootElement { get { return Root as JArray; } }
        public ArrayChangeTracking() { }
        public ArrayChangeTracking(JArray root, Dictionary<string, object> context) : base(root, context) { }
        public override string ToString()
        {
            return Root == null ? "[]" : Root.ToString(Formatting.Indented);
        }
    }
}