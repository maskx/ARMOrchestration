using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.ARMTemplate
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DependsOnCollection : ChangeTracking, IEnumerable<string>
    {
        [JsonProperty]
        private readonly List<string> _List = new List<string>();

        public string this[int index]
        {
            get { return _List[index]; }
        }

        public int Count => _List.Count;

        public bool IsReadOnly => true;

        public void Add(string item, Deployment deployment)
        {
            if (string.IsNullOrEmpty(item))
                return;
            if (!Contains(item))
            {
                var resources = deployment.GetResources(item);
                if (resources.Count == 0)
                    throw new Exception($"cannot find dependson resource named '{item}'");
                if (resources.Count > 1)
                    throw new Exception($"with the name of '{item}' find more than one resourc in the template");
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/define-resource-dependency#dependson
                // When a conditional resource isn't deployed, Azure Resource Manager automatically removes it from the required dependencies.
                var r = resources[0];
                if (r.Condition)
                {
                    Change(null, null);
                    if (r.Copy != null && !r.CopyIndex.HasValue)
                        _List.Add(r.Copy.NameWithServiceType);
                    else
                        _List.Add(r.NameWithServiceType);
                }

            }
        }

        public void AddRange(IEnumerable<string> collection, Deployment deployment)
        {
            foreach (var item in collection)
            {
                Add(item, deployment);
            }
        }

        public void Change(object value, string name = "")
        {
           // todo: collection Chage
        }

        public void Clear()
        {
            _List.Clear();
            Change(null, null);
        }

        public bool Contains(string item)
        {
            return IndexOf(item) != -1;
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _List.CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _List.GetEnumerator();
        }

        public int IndexOf(string item)
        {
            var n_s = item.Split('/');
            return _List.FindIndex((str) =>
            {
                if (item.Length == str.Length)
                    return item == str;
                var c_s = str.Split('/');
                int c = c_s.Length - 1;
                int n = n_s.Length - 1;
                while (n >= 0 && c >= 0)
                {
                    if (c_s[c] != n_s[n]) return false;
                    if (c_s.Length > n_s.Length)
                    {
                        c -= 2;
                        n -= 1;
                    }
                    else
                    {
                        c -= 1;
                        n -= 2;
                    }
                }
                return true;
            });
        }

        public bool Remove(string item)
        {
            int index = IndexOf(item);
            if (index == -1)
                return false;
            _List.RemoveAt(index);
            Change(null, null);
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _List.GetEnumerator();
        }
        public override string ToString()
        {
            if (_List.Count == 0)
                return "[]";
            return $"[\"{string.Join("\",\"", _List)}\"]";
        }
    }
}