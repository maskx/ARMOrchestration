﻿using Dynamitey.DynamicObjects;
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
            string n_name = string.Empty;
            string n_fullName = string.Empty;
            string n_nameWithServiceType = string.Empty;

            var n_s = item.Split('/');
            n_name = n_s[^1];
            if (n_s[0].IndexOf('.') > 0)
            {
                n_nameWithServiceType = item;
                n_fullName = n_s[2];
                for (int i = 4; i < n_s.Length;)
                {
                    n_fullName += "/" + n_s[i];
                    i += 2;
                }
            }
            else
                n_fullName = item;

            return _List.FindIndex((str) =>
            {
                string c_name = string.Empty;
                string c_fullName = string.Empty;
                string c_nameWithServiceType = string.Empty;

                var c_s = str.Split('/');
                c_name = c_s[^1];
                if (c_name != n_name)
                    return false;
                if (c_s[0].IndexOf('.') > 0)
                {
                    c_nameWithServiceType = str;
                    c_fullName = c_s[2];
                    for (int i = 4; i < c_s.Length;)
                    {
                        c_fullName += "/" + c_s[i];
                        i += 2;
                    }
                }
                else
                    c_fullName = str;
                if (c_fullName != n_fullName)
                    return false;
                if (string.IsNullOrEmpty(c_nameWithServiceType) || string.IsNullOrEmpty(n_nameWithServiceType))
                    return true;
                if (c_nameWithServiceType == n_nameWithServiceType)
                    return true;
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