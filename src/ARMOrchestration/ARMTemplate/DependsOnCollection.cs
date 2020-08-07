using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.ARMTemplate
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DependsOnCollection : IEnumerable<string>
    {
        [JsonProperty]
        private List<string> _List = new List<string>();

        public string this[int index]
        {
            get { return _List[index]; }
        }

        public int Count => _List.Count;

        public bool IsReadOnly => true;

        public void Add(string item, ResourceCollection resources)
        {
            if (!Contains(item))
            {
                if (!resources.TryGetValue(item, out Resource r))
                    throw new Exception($"cannot find dependson resource named '{item}'");
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/define-resource-dependency#dependson
                // When a conditional resource isn't deployed, Azure Resource Manager automatically removes it from the required dependencies.
                if (r.Condition)
                    _List.Add(item);
            }
        }

        public void AddRange(IEnumerable<string> collection, ResourceCollection resources)
        {
            foreach (var item in collection)
            {
                Add(item, resources);
            }
        }

        public void Clear()
        {
            _List.Clear();
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
            var n1 = string.Empty;
            var n2 = item;
            var n_index = n2.LastIndexOf('/');
            if (n_index > 0)
            {
                n1 = n2.Substring(0, n_index);
                n2 = n2.Substring(n_index + 1, n2.Length - n_index - 1);
            }
            return _List.FindIndex((str) =>
            {
                var c1 = string.Empty;
                var c2 = str;
                var c_index = str.LastIndexOf('/');
                if (c_index > 0)
                {
                    c1 = c2.Substring(0, c_index);
                    c2 = c2.Substring(c_index + 1, c2.Length - c_index - 1);
                }
                if (c2 == n2)
                {
                    if (string.IsNullOrEmpty(c1))
                        return true;
                    if (string.IsNullOrEmpty(n1))
                        return true;
                    if (c1 == n1)
                        return true;
                }
                return false;
            });
        }

        public bool Remove(string item)
        {
            int index = IndexOf(item);
            if (index == -1)
                return false;
            _List.RemoveAt(index);
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _List.GetEnumerator();
        }
    }
}