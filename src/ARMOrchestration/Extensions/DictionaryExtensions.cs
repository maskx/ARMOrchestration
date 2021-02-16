using maskx.ARMOrchestration.Functions;
using System.Collections.Generic;

namespace maskx.ARMOrchestration.Extensions
{
    public static class DictionaryExtensions
    {
        public static Dictionary<K, V> CopyNew<K, V>(this Dictionary<K, V> source)
        {
            Dictionary<K, V> rtv = new Dictionary<K, V>();
            foreach (var k in source.Keys)
            {
                rtv.Add(k, source[k]);
            }
            return rtv;
        }
        public static Dictionary<string,object> AppendPath(this Dictionary<string, object> source,string segment)
        {
            if (source.TryGetValue(ContextKeys.PATH, out object p))
                source[ContextKeys.PATH] = $"{p}/{segment}";
            else
                source[ContextKeys.PATH] = segment;
            return source;
        }
    }
}
