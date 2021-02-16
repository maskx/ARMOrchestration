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
        public Deployment Deployment { get; set; }
        public IServiceProvider ServiceProvider { get { return Deployment.ServiceProvider; } }
        public ARMFunctions Functions { get { return ServiceProvider.GetService<ARMFunctions>(); } }
        private Dictionary<string, object> _FullContext;
        internal virtual Dictionary<string, object> FullContext
        {
            get
            {
                if (_FullContext == null)
                {
                    _FullContext = new Dictionary<string, object> {
                        {ContextKeys.ARM_CONTEXT,this.Deployment} };
                    foreach (var item in Deployment.Context)
                    {
                        if (item.Key == ContextKeys.ARM_CONTEXT) continue;
                        _FullContext.Add(item.Key, item.Value);
                    }
                }
                return _FullContext;
            }
        }
        public string JsonPath
        {
            get { return Root.Path; }
        }
        public ChangeTracking()
        {

        }
        public ChangeTracking(JToken root, Dictionary<string, object> context)
        {

            this.Deployment = context[ContextKeys.ARM_CONTEXT] as Deployment;
            this.Root = root;
        }

        internal JToken Root
        {
            get; set;
        }

        public void Change(object value, string name)
        {
            // TODO: Change
            // this.Input.ChangedResoures.
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
                    o = this.Functions.Evaluate(e.Value<string>(), FullContext, $"{RootElement.Path}.{name}");
                else if (e.Type == JTokenType.Boolean)
                    o = e.Value<bool>();
                else if (e.Type == JTokenType.Integer)
                    o = e.Value<int>();
                return (T)o;
            }
            else
                return default;
        }
    }
    public class ArrayChangeTracking : ChangeTracking
    {
        public JArray RootElement { get { return Root as JArray; } }
        public ArrayChangeTracking() { }
        public ArrayChangeTracking(JArray root, Dictionary<string, object> context) : base(root, context) { }
    }
}