using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.Text.Json;

namespace maskx.OrchestrationCreator
{
    public class JsonValue : DynamicObject, IDisposable
    {
        public string RawString { get; set; }
        private JsonDocument json = null;

        public JsonValue(string rawString)
        {
            this.RawString = rawString;
        }
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (json == null)
                json = JsonDocument.Parse(RawString);
            int i = 0;
            int index = (int)indexes[0];
            foreach (var item in json.RootElement.EnumerateArray())
            {
                if (i == index)
                {
                    result = GetElementValue(item);
                    return true;
                }
                i++;
            }
            result = null;
            return false;
        }
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (json == null)
                json = JsonDocument.Parse(RawString);
            if (json.RootElement.TryGetProperty(binder.Name, out JsonElement element))
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
                    return element.GetDecimal();
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
            if (json == null)
                json = JsonDocument.Parse(RawString);
            string[] p = path.Split('/');
            JsonElement ele = json.RootElement;
            foreach (var item in p)
            {
                if (!ele.TryGetProperty(item, out ele))
                    return string.Empty;
            }
            return ele.GetRawText();
        }
        public bool Contains(object item)
        {
            if (json == null)
                json = JsonDocument.Parse(RawString);
            var root = json.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                Type t = item.GetType();
                foreach (var element in json.RootElement.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String && element.GetString() == item.ToString())
                    {
                        return true;
                    }
                    if (element.ValueKind == JsonValueKind.Number)
                    {
                        if (t == typeof(int) && element.GetInt64() == (Int64)item)
                            return true;
                        else if (element.GetDecimal() == (decimal)item)
                            return true;
                    }
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var obj in json.RootElement.EnumerateObject())
                {
                    if (obj.Name == item.ToString())
                        return true;
                }
            }
            return false;
        }

    }
}