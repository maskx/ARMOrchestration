using System;
using System.Collections;
using System.Collections.Generic;

namespace maskx.ARMOrchestration
{
    public class ChangeMap : IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _InnerDictionary = new Dictionary<string, object>();
        public object this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ICollection<string> Keys => _InnerDictionary.Keys;

        public ICollection<object> Values => _InnerDictionary.Values;

        public int Count => _InnerDictionary.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            _InnerDictionary.Add(key,value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _InnerDictionary.Add(item.Key,item.Value);
        }

        public void Clear()
        {
            _InnerDictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            if(_InnerDictionary.TryGetValue(item.Key,out object v))
            {
                return v == item.Value;
            }
            return false;
        }

        public bool ContainsKey(string key)
        {
            return _InnerDictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            (_InnerDictionary as ICollection<KeyValuePair<string, object>>).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _InnerDictionary.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _InnerDictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            if(_InnerDictionary.TryGetValue(item.Key,out object v))
            {
                if(v==item.Value)
                {
                    _InnerDictionary.Remove(item.Key);
                    return true;
                }
            }
            return false;
        }

        public bool TryGetValue(string key, out object value)
        {
            return _InnerDictionary.TryGetValue(key,out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _InnerDictionary.GetEnumerator();
        }
    }
}
