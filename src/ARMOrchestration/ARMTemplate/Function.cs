using maskx.ARMOrchestration.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class Functions
    {
        public static Functions Parse(JsonElement root)
        {
            Functions functions = new Functions();
            foreach (var funcDef in root.EnumerateArray())
            {
                if (funcDef.TryGetProperty("namespace", out JsonElement nsEle))
                {
                    functions.Members.Add(nsEle.GetString(), ARMTemplate.Members.Parse(funcDef.GetProperty("members")));

                }
            }
            return functions;
        }
        public override string ToString()
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartArray();
            foreach (var key in this.Members.Keys)
            {
                writer.WriteRawString(key,this.Members[key].ToString());
            }
            writer.WriteEndArray();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        public Dictionary<string, Members> Members { get; set; } = new Dictionary<string, Members>();

        public Members this[string name]
        {
            get
            {
                return this.Members[name];
            }
        }
    }

    public class Member
    {
        public static Member Parse(JsonElement root)
        {
            return new Member()
            {
                Parameters = root.GetProperty("parameters").GetRawText(),
                Output = root.GetProperty("output").GetRawText()
            };
        }
        public override string ToString()
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            writer.WriteRawString("parameters", this.Parameters);
            writer.WriteRawString("output", this.Output);
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        public string Parameters { get; set; }

        public string Output { get; set; }
    }

    public class Members
    {
        public static Members Parse(JsonElement root)
        {
            Members members = new Members();
            foreach (var m in root.EnumerateObject())
            {
                members.Member.Add(m.Name, ARMTemplate.Member.Parse(m.Value));
            }
            return members;
        }
        public override string ToString()
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();
            foreach (var key in this.Member.Keys)
            {
                writer.WriteRawString(key,this.Member[key].ToString());
            }
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        public Dictionary<string, Member> Member = new Dictionary<string, Member>();

        public Member this[string name]
        {
            get
            {
                return this.Member[name];
            }
        }
    }
}