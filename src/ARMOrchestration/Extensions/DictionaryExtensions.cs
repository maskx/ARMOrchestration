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
        public static Dictionary<string, object> AppendSegment(this Dictionary<string, object> source, string segment)
        {
            if (string.IsNullOrEmpty(segment))
                return source;
            if (source.TryGetValue(ContextKeys.FUNCTION_PATH, out object p))
                source[ContextKeys.FUNCTION_PATH] = $"{p}/{segment}";
            else
                source[ContextKeys.FUNCTION_PATH] = segment;
            return source;
        }
        public static Dictionary<string, object> RemoveLastSegment(this Dictionary<string, object> source)
        {
            if (source.TryGetValue(ContextKeys.FUNCTION_PATH, out object p))
            {
                var d = p.ToString().Split('/');
                if (d.Length == 1)
                    source.Remove(ContextKeys.FUNCTION_PATH);
                else
                {
                    source[ContextKeys.FUNCTION_PATH] = string.Join('/', d, 0, d.Length - 1);
                }
            }
            return source;
        }
    }
}
