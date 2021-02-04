using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SqlServer.Management.Dmf;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class ResourceCollection : ICollection<Resource>, IChangeTracking
    {
        private Deployment _Input { get { return _FullContext[ContextKeys.ARM_CONTEXT] as Deployment; } }
        private IServiceProvider _ServiceProvider => _Input.ServiceProvider;

        private long _OldVersion;
        private long _NewVersion;

        public long TrackingVersion
        {
            get { return _NewVersion; }
            set { _OldVersion = _NewVersion = value; }
        }

        public bool HasChanged
        {
            get
            {
                if (_OldVersion != _NewVersion)
                    return true;
                SyncAllChanges();
                if (_OldVersion != _NewVersion)
                    return true;
                return false;
            }
        }

        private void SyncAllChanges()
        {
            foreach (var r in _Resources)
            {
                if (r is IChangeTracking ct)
                {
                    if (ct.TrackingVersion != this.TrackingVersion)
                    {
                        this.TrackingVersion = DateTime.Now.Ticks;
                    }
                }
            }
        }

        public bool Accepet(long newVersion = 0)
        {
            if (!HasChanged)
                return false;
            if (newVersion == 0)
                newVersion = this.TrackingVersion;

            foreach (var r in _Resources)
            {
                if (r.TrackingVersion != newVersion)
                    r.Accepet(this.TrackingVersion);
            }

            this.TrackingVersion = newVersion;
            return false;
        }

        public void Change(object value, string name)
        {
            this._NewVersion = DateTime.Now.Ticks;
        }
        private readonly Dictionary<string, object> _FullContext;
        private readonly string _ParentName;
        private readonly string _ParentType;
        private void ExpandResource(string content)
        {
            using var json = JsonDocument.Parse(content);
            this._Resources.Clear();
            this._Deployments?.Clear();
            foreach (var resource in json.RootElement.EnumerateArray())
            {
                var r = new Resource(resource.GetRawText(), _FullContext, _ParentName, _ParentType)
                {
                    TrackingVersion = this.TrackingVersion
                };
                Add(r);
            }
        }

        public ResourceCollection(string rawString, Dictionary<string, object> fullContext, string parentName = null, string parentType = null)
        {
            this._FullContext = fullContext;
            this._ParentName = parentName;
            this._ParentType = parentType;
            ExpandResource(rawString);
        }

        public virtual string RawString
        {
            get
            {
                this.Accepet();
                using MemoryStream ms = new MemoryStream();
                using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
                writer.WriteStartArray();
                foreach (var r in _Resources)
                {
                    r.Accepet(this.TrackingVersion);
                    writer.WriteRawString(r.ToString());
                }
                writer.WriteEndArray();
                writer.Flush();
                return Encoding.UTF8.GetString(ms.ToArray());
            }
            set
            {
                ExpandResource(value);
                Change(null, null);
            }
        }
        private readonly List<Resource> _Resources = new List<Resource>();
        private List<Deployment> _Deployments = null;

        public Resource this[string name]
        {
            get
            {
                if (TryGetValue(name, out Resource r))
                    return r;
                return null;
            }
        }

        public bool TryGetValue(string name, out Resource resource)
        {
            foreach (var r in _Resources)
            {
                if (r.ResourceId.EndsWith(name) || r.FullName.EndsWith(name))
                {
                    resource = r;
                    return true;
                }
            }
            resource = null;
            return false;
        }

        public int Count
        {
            get
            {
                return _Resources.Count;
            }
        }

        public bool IsReadOnly => false;

        public void Add(Resource item)
        {
            if (Contains(item))
            {
                throw new Exception($"already exists:{item.ResourceId}");
            }
            else
            {
                item.Input = _Input;
                _Resources.Add(item);
                Change(null, null);
            }
        }

        public void Clear()
        {
            this._Resources.Clear();
            this._Deployments?.Clear();
            Change(null, null);
        }



        public bool Contains(Resource item)
        {
            foreach (var r in _Resources)
            {
                if (r.ResourceId.Equals(item.ResourceId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public void CopyTo(Resource[] array, int arrayIndex)
        {
            _Resources.CopyTo(array, arrayIndex);
        }

        public IEnumerator<Resource> GetEnumerator()
        {
            return _Resources.GetEnumerator();
        }

        public bool Remove(Resource item)
        {
            foreach (var r in _Resources)
            {
                if (r.ResourceId.Equals(item.ResourceId, StringComparison.OrdinalIgnoreCase))
                {
                    _Resources.Remove(r);
                    return true;
                }
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _Resources.GetEnumerator();
        }

        public override string ToString()
        {
            return this.RawString;
        }

        public IEnumerable<Deployment> EnumerateDeployments()
        {
            bool needInit = false;
            if (this._Deployments == null)
            {
                needInit = true;
                this._Deployments = new List<Deployment>();
            }
            if (this.HasChanged || needInit)
            {
                var infra = _ServiceProvider.GetService<IInfrastructure>();
                this._Deployments.Clear();
                foreach (var item in _Resources)
                {
                    if (item.Type == infra.BuiltinServiceTypes.Deployments)
                    {
                        this._Deployments.Add(Deployment.Parse(item));
                    }
                }
            }
            foreach (var item in this._Deployments)
            {
                yield return item;
                foreach (var n in item.EnumerateDeployments())
                {
                    yield return n;
                }
            }
        }
    }
}