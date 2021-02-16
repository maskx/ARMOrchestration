using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class ResourceCollection : ArrayChangeTracking, ICollection<Resource>
    {
        private readonly string _ParentName;
        private readonly string _ParentType;
        private void ExpandResource()
        {
            this._Resources.Clear();
            this._Deployments?.Clear();
            foreach (var resource in RootElement.Children())
            {
                var r = new Resource(resource as JObject, FullContext, _ParentName, _ParentType)
                {

                };
                Add(r);
            }
        }
        public ResourceCollection()
        {

        }
        public ResourceCollection(JArray root, Dictionary<string, object> fullContext, string parentName = null, string parentType = null) : base(root, fullContext)
        {
            this._ParentName = parentName;
            this._ParentType = parentType;
            ExpandResource();
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
                item.Deployment = Deployment;
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


        public IEnumerable<Deployment> EnumerateDeployments()
        {
            bool needInit = false;
            if (this._Deployments == null)
            {
                needInit = true;
                this._Deployments = new List<Deployment>();
            }
            // TODO: asdfasdfasdf
            //if (this.HasChanged || needInit)
            //{
            //    var infra = ServiceProvider.GetService<IInfrastructure>();
            //    this._Deployments.Clear();
            //    foreach (var item in _Resources)
            //    {
            //        if (item.Type == infra.BuiltinServiceTypes.Deployments)
            //        {
            //            this._Deployments.Add(Deployment.Parse(item));
            //        }
            //    }
            //}
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