using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Functions;
using Microsoft.Extensions.DependencyInjection;
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
    public class ResourceCollection : ICollection<Resource>, IChangeTracking, IDisposable
    {
        private Deployment _Input;
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
            foreach (var item in _Resources.Values)
            {
                foreach (var r in item)
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
        }

        public bool Accepet(long newVersion = 0)
        {
            if (!HasChanged)
                return false;
            if (newVersion == 0)
                newVersion = this.TrackingVersion;
            foreach (var rs in _Resources.Values)
            {
                foreach (var r in rs)
                {
                    if (r.TrackingVersion != newVersion)
                        r.Accepet(this.TrackingVersion);
                }
            }
            this.TrackingVersion = newVersion;
            return false;
        }

        public void Change(object value, string name)
        {
            this._NewVersion = DateTime.Now.Ticks;
        }

        private void ExpandResource(JsonElement element, Dictionary<string, object> fullContext, string parentName = null, string parentType = null)
        {
            _Input = fullContext[ContextKeys.ARM_CONTEXT] as Deployment;

            foreach (var resource in element.EnumerateArray())
            {
                var r = new Resource(resource.GetRawText(), fullContext, parentName, parentType)
                {
                    TrackingVersion = this.TrackingVersion
                };
                Add(r);
            }
        }

        internal JsonElement RootElement;

        public ResourceCollection(string rawString, Dictionary<string, object> fullContext, string parentName = null, string parentType = null)
        {
            this.RawString = rawString;
            ExpandResource(this.RootElement, fullContext, parentName, parentType);
        }

        public virtual string RawString
        {
            get
            {
                this.Accepet();
                return RootElement.GetRawText();
            }
            set
            {
                if (json != null)
                    json.Dispose();
                json = JsonDocument.Parse(value);
                RootElement = json.RootElement;
            }
        }

        private JsonDocument json;
        private readonly ConcurrentDictionary<string, List<Resource>> _Resources = new ConcurrentDictionary<string, List<Resource>>();
        private List<Deployment> _Deployments = null;

        public Resource this[string name]
        {
            get
            {
                int index = name.LastIndexOf('/');
                if (index > 0)
                {
                    string n = name.Substring(index + 1);
                    if (!this._Resources.TryGetValue(n, out List<Resource> rs))
                        throw new KeyNotFoundException(name);
                    bool withServiceType = name.Contains('.');
                    foreach (var r in rs)
                    {
                        if (withServiceType && r.ResourceId.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                            return r;
                        if (!withServiceType && r.Name == name)
                            return r;
                    }
                    throw new KeyNotFoundException(name);
                }
                else
                {
                    if (!this._Resources.TryGetValue(name, out List<Resource> rs))
                        throw new KeyNotFoundException(name);
                    if (rs.Count > 1)
                        throw new Exception($"more than one resource have named '{name}',try to get resource by fullname or inclued Servcie Types)");
                    return rs[0];
                }
            }
            set
            {
                Remove(value);
                Add(value);
            }
        }

        public bool TryGetValue(string name, out Resource resource)
        {
            resource = null;
            int index = name.LastIndexOf('/');
            if (index > 0)
            {
                string n = name.Substring(index + 1);
                if (!this._Resources.TryGetValue(n, out List<Resource> rs))
                    return false;
                bool withServiceType = name.Contains('.');
                foreach (var r in rs)
                {
                    if (withServiceType && r.ResourceId.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                    {
                        resource = r;
                        return true;
                    }
                    if (!withServiceType && r.Name == name)
                    {
                        resource = r;
                        return true;
                    }
                }
                return false;
            }
            else
            {
                if (!this._Resources.TryGetValue(name, out List<Resource> rs))
                    return false;
                if (rs.Count > 1)
                    return false;
                //throw new Exception($"more than one resource have named '{name}',try to get resource by fullname or inclued Servcie Types)");
                resource = rs[0];
                return true;
            }
        }
       
        public int Count
        {
            get
            {
                return _Resources.Values.Sum((rs) => { return rs.Count; });
            }
        }

        public bool IsReadOnly => false;

        public void Add(Resource item)
        {
            string name = item.Name;
            if (item.Copy != null && !item.CopyIndex.HasValue)
                name = item.Copy.Name;
            int index = name.LastIndexOf('/');
            if (index > 0)
                name = name.Substring(index + 1);
            if (!_Resources.TryGetValue(name, out List<Resource> rs))
            {
                rs = new List<Resource>();
                _Resources.TryAdd(name, rs);
            }
            foreach (var r in rs)
            {
                // item already in clollection
                if (r.ResourceId.Equals(item.ResourceId, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            item.Input = _Input;
            rs.Add(item);
            Change(null, null);
        }

        public void Clear()
        {
            this._Resources.Clear();
            this._Deployments.Clear();
            Change(null, null);
        }

        public bool ContainsKey(string name)
        {
            int index = name.LastIndexOf('/');
            if (index < 0)
                return this._Resources.ContainsKey(name);
            string n = name.Substring(index + 1);
            if (!this._Resources.TryGetValue(n, out List<Resource> rs))
                return false;
            bool withServiceType = name.Contains('.');
            foreach (var r in rs)
            {
                if (withServiceType && r.ResourceId.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (!withServiceType && r.Name == name)
                    return true;
            }
            return false;
        }

        public bool Contains(Resource item)
        {
            return ContainsKey(item.Name);
        }

        public void CopyTo(Resource[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException();
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException();
            if (array.Length - arrayIndex < this.Count)
                throw new ArgumentException("The number of elements in the source is greater than the available space from arrayIndex to the end of the destination array");
            int i = arrayIndex;
            foreach (var rs in this._Resources.Values)
            {
                foreach (var r in rs)
                {
                    array[i] = r;
                    i++;
                }
            }
        }

        public IEnumerator<Resource> GetEnumerator()
        {
            foreach (var rs in _Resources.Values)
            {
                foreach (var r in rs)
                {
                    yield return r;
                }
            }
        }

        public bool Remove(Resource item)
        {
            string name = item.Name;
            int index = name.LastIndexOf('/');
            if (index > 0)
                name = name.Substring(index + 1);
            if (!this._Resources.TryGetValue(name, out List<Resource> rs))
                return false;
            if (rs.Count == 1)
            {
                Change(null, null);
                return this._Resources.TryRemove(name, out _);
            }
            foreach (var r in rs)
            {
                if (r.Name == item.Name)
                {
                    Change(null, null);
                    return rs.Remove(r);
                }
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var rs in _Resources.Values)
            {
                foreach (var r in rs)
                {
                    yield return r;
                }
            }
        }

        public override string ToString()
        {
            this.Accepet();
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartArray();
            foreach (var item in _Resources.Values)
            {
                foreach (var r in item)
                {
                    r.Accepet(this.TrackingVersion);
                    writer.WriteRawString(r.ToString());
                }
            }
            writer.WriteEndArray();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public void Dispose()
        {
            if (json != null)
                json.Dispose();
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
                foreach (var item in this)
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