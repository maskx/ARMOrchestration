using maskx.ARMOrchestration.Extensions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;

namespace maskx.ARMOrchestration
{
    public class JsonValue : DynamicObject, IDisposable
    {
        public string RawString { get; set; }
        private JsonDocument json = null;

        private JsonElement RootElement
        {
            get
            {
                if (json == null)
                    json = JsonDocument.Parse(RawString);
                return json.RootElement;
            }
        }

        public JsonValueKind ValueKind
        {
            get
            {
                return RootElement.ValueKind;
            }
        }

        public JsonValue(string rawString)
        {
            this.RawString = rawString;
        }

        public object this[int index]
        {
            get
            {
                return GetElementValue(RootElement[index]);
            }
        }

        public object this[string name]
        {
            get
            {
                if (RootElement.TryGetProperty(name, out JsonElement ele))
                {
                    return GetElementValue(ele);
                }
                throw new IndexOutOfRangeException();
            }
        }

        public int Length
        {
            get
            {
                if (RootElement.ValueKind == JsonValueKind.Array)
                    return RootElement.GetArrayLength();
                else if (RootElement.ValueKind == JsonValueKind.Object)
                    return RootElement.EnumerateObject().Count();
                return 0;
            }
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            try
            {
                if (indexes[0] is string)
                {
                    result = this[(string)indexes[0]];
                }
                else
                {
                    result = this[(int)indexes[0]];
                }
                return true;
            }
            catch
            {
            }
            result = null;
            return false;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (RootElement.TryGetProperty(binder.Name, out JsonElement element))
            {
                result = GetElementValue(element);
                return true;
            }
            result = null;
            return false;
        }

        public void Dispose()
        {
            if (json != null)
                json.Dispose();
        }

        public static object GetElementValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Undefined:
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return new JsonValue(element.GetRawText());

                case JsonValueKind.String:
                    return element.GetString();

                case JsonValueKind.Number:
                    return element.GetInt32();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                    return null;

                default:
                    return element.GetRawText();
            }
        }

        public override string ToString()
        {
            return this.RawString;
        }

        public string GetNodeStringValue(string path)
        {
            string[] p = path.Split('/');
            JsonElement ele = RootElement;
            foreach (var item in p)
            {
                if (!ele.TryGetProperty(item, out ele))
                    return string.Empty;
            }
            return ele.GetRawText();
        }

        public bool Contains(object item)
        {
            if (RootElement.ValueKind == JsonValueKind.Array)
            {
                var jv = item as JsonValue;
                foreach (var element in RootElement.EnumerateArray())
                {
                    if (jv != null)
                    {
                        if (element.ValueKind == JsonValueKind.Array || element.ValueKind == JsonValueKind.Object)
                        {
                            if (element.IsEqual(jv.RootElement))
                                return true;
                        }
                    }
                    else if (element.ValueKind == JsonValueKind.String && element.ValueEquals(item.ToString()))
                        return true;
                    else if (element.GetRawText() == item.ToString())
                        return true;
                }
            }
            else if (RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var obj in RootElement.EnumerateObject())
                {
                    if (obj.Name == item.ToString())
                        return true;
                }
            }
            return false;
        }

        public JsonValue Intersect(JsonValue target)
        {
            var s = this.RootElement;
            var t = target.RootElement;
            if (s.ValueKind == JsonValueKind.Array)
            {
                var newJsonStr = string.Join(",",
                      s.EnumerateArray()
                         .Intersect(t.EnumerateArray(), new JsonElementEqualityComparer())
                         .Select((e) =>
                         {
                             return e.GetRawText();
                         }));
                return new JsonValue($"[{newJsonStr}]");
            }
            else if (s.ValueKind == JsonValueKind.Object)
            {
                List<string> newObjList = new List<string>();
                foreach (var item in s.EnumerateObject())
                {
                    if (t.TryGetProperty(item.Name, out JsonElement ele)
                        && item.Value.IsEqual(ele))
                    {
                        newObjList.Add($"\"{item.Name}\":{ele.GetRawText()}");
                    }
                }
                string newObjString = string.Join(",", newObjList);
                return new JsonValue($"{{{newObjString}}}");
            }
            return null;
        }

        public JsonValue Union(JsonValue target)
        {
            var s = this.RootElement;
            var t = target.RootElement;
            if (s.ValueKind == JsonValueKind.Array)
            {
                var newJsonStr = string.Join(",",
                      s.EnumerateArray()
                         .Union(t.EnumerateArray(), new JsonElementEqualityComparer())
                         .Select((e) =>
                         {
                             return e.GetRawText();
                         }));
                return new JsonValue($"[{newJsonStr}]");
            }
            else if (s.ValueKind == JsonValueKind.Object)
            {
                Dictionary<string, object> newObjList = new Dictionary<string, object>();
                foreach (var item in s.EnumerateObject())
                {
                    newObjList.Add(item.Name, item.Value.GetRawText());
                }
                foreach (var item in t.EnumerateObject())
                {
                    newObjList[item.Name] = item.Value.GetRawText();
                }
                return new JsonValue($"{{{string.Join(",", newObjList.Select(k => $"\"{k.Key}\":{k.Value}"))}}}");
            }
            return null;
        }

        public static string PackageJson(object value)
        {
            if (value is string s)
                return $"\"{s.GetRawString()}\"";
            else if (value is bool b)
                return b ? "true" : "false";
            else
                return value.ToString();
        }

        public object Max()
        {
            return RootElement.EnumerateArray().Max((e) =>
            {
                //ARM only support int in array
                return e.GetInt64();
            });
        }

        public object Min()
        {
            return RootElement.EnumerateArray().Min((e) =>
            {
                //ARM only support int in array
                return e.GetInt64();
            });
        }

        public override bool Equals(object obj)
        {
            if (obj is JsonValue jv)
            {
                return this.RootElement.IsEqual(jv.RootElement);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return 1305398700 + EqualityComparer<string>.Default.GetHashCode(RawString);
        }
    }
}