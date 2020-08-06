using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class ResourceCollection : ICollection<Resource>
    {
        private readonly ConcurrentDictionary<string, List<Resource>> _Resources = new ConcurrentDictionary<string, List<Resource>>();

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
                    foreach (var r in rs)
                    {
                        if (r.ResourceId.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                            return r;
                    }
                    throw new KeyNotFoundException(name);
                }
                else
                {
                    if (!this._Resources.TryGetValue(name, out List<Resource> rs))
                        throw new KeyNotFoundException(name);
                    if (rs.Count > 1)
                        throw new Exception($"more than one resource have named '{name}',try to get resource by fullname (inclued Servcie Types)");
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
                foreach (var r in rs)
                {
                    if (r.ResourceId.EndsWith(name, StringComparison.OrdinalIgnoreCase))
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
            if (!_Resources.TryGetValue(item.Name, out List<Resource> rs))
            {
                rs = new List<Resource>();
                _Resources.TryAdd(item.Name, rs);
            }
            foreach (var r in rs)
            {
                // item already in clollection
                if (r.ResourceId.Equals(item.ResourceId, StringComparison.OrdinalIgnoreCase))
                    return;
            }
            rs.Add(item);
        }

        public void Clear()
        {
            this._Resources.Clear();
        }

        public bool ContainsKey(string name)
        {
            int index = name.LastIndexOf('/');
            if (index < 0)
                return this._Resources.ContainsKey(name);
            string n = name.Substring(index + 1);
            if (!this._Resources.TryGetValue(n, out List<Resource> rs))
                return false;
            foreach (var r in rs)
            {
                if (r.ResourceId.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public bool Contains(Resource item)
        {
            if (!_Resources.TryGetValue(item.Name, out List<Resource> rs))
                return false;
            foreach (var r in rs)
            {
                if (r.ResourceId.Equals(item.FullName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
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
            if (!this._Resources.TryGetValue(item.Name, out List<Resource> rs))
                return false;
            foreach (var r in rs)
            {
                if (r.FullName == item.FullName)
                {
                    if (rs.Count == 1)
                        return this._Resources.TryRemove(item.Name, out _);
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
    }
}