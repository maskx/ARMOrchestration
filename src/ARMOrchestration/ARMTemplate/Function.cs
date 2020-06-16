using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class Functions
    {
        public static (bool Result, string Message, Functions Functions) Parse(string jsonString)
        {
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;
            Functions functions = new Functions();
            foreach (var funcDef in root.EnumerateArray())
            {
                if (funcDef.TryGetProperty("namespace", out JsonElement nsEle))
                {
                    var mr = ARMTemplate.Members.Parse(funcDef.GetProperty("members").GetRawText());
                    if (mr.Result)
                        functions.Members.Add(nsEle.GetString(), mr.Members);
                    else
                        return (false, mr.Message, null);
                }
            }
            return (true, string.Empty, functions);
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
        public static (bool Result, string Message, Member Member) Parse(string jsonString)
        {
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;
            return (true, string.Empty, new Member()
            {
                Parameters = root.GetProperty("parameters").GetRawText(),
                Output = root.GetProperty("output").GetRawText()
            });
        }

        public string Parameters { get; set; }

        public string Output { get; set; }
    }

    public class Members
    {
        public static (bool Result, string Message, Members Members) Parse(string jsonString)
        {
            Members members = new Members();
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;
            foreach (var m in root.EnumerateObject())
            {
                var mr = Member.Parse(m.Value.GetRawText());
                if (mr.Result)
                    members.Memeber.Add(m.Name, mr.Member);
                else
                    return (false, mr.Message, null);
            }
            return (true, string.Empty, members);
        }

        public Dictionary<string, Member> Memeber = new Dictionary<string, Member>();

        public Member this[string name]
        {
            get
            {
                return this.Memeber[name];
            }
        }
    }
}