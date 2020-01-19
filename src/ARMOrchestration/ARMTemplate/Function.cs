using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class Functions : IDisposable
    {
        public Functions()
        {
        }

        public static (bool Result, string Message, Functions Functions) Parse(string jsonString)
        {
            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;
            Functions functions = new Functions();
            foreach (var funcDef in root.EnumerateArray())
            {
                if (funcDef.TryGetProperty("namespace", out JsonElement nsEle))
                {
                    var mr = Members.Parse(funcDef.GetProperty("members").GetRawText());
                    if (mr.Result)
                        functions.innerMembers.Add(nsEle.GetString(), mr.Members);
                    else
                        return (false, mr.Message, null);
                }
            }
            return (true, string.Empty, functions);
        }

        private Dictionary<string, Members> innerMembers { get; set; } = new Dictionary<string, Members>();

        public Members this[string name]
        {
            get
            {
                return this.innerMembers[name];
            }
        }

        private string jsonString;

        public Functions(string jsonString)
        {
            this.jsonString = jsonString;
        }

        private JsonDocument jsonDoc;

        private JsonElement root
        {
            get
            {
                if (jsonDoc == null)
                    jsonDoc = JsonDocument.Parse(jsonString);
                return jsonDoc.RootElement;
            }
        }

        public void Dispose()
        {
            if (this.jsonDoc != null)
                this.jsonDoc.Dispose();
        }
    }

    public class Member : IDisposable
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

        public Member()
        {
        }

        public string Parameters { get; set; }

        public string Output { get; set; }

        private string jsonString;
        private JsonDocument jsonDoc;

        private JsonElement root
        {
            get
            {
                if (jsonDoc == null)
                    jsonDoc = JsonDocument.Parse(jsonString);
                return jsonDoc.RootElement;
            }
        }

        public Member(string jsonString)
        {
            this.jsonString = jsonString;
        }

        public void Dispose()
        {
            if (this.jsonDoc != null)
                this.jsonDoc.Dispose();
        }
    }

    public class Members : IDisposable
    {
        public Members()
        {
        }

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

        private Dictionary<string, Member> Memeber = new Dictionary<string, Member>();

        public Member this[string name]
        {
            get
            {
                foreach (var m in root.EnumerateObject())
                {
                    if (m.Name == name)
                        return new Member(m.Value.GetRawText());
                }
                return null;
            }
        }

        private string jsonString;
        private JsonDocument jsonDoc;

        private JsonElement root
        {
            get
            {
                if (jsonDoc == null)
                    jsonDoc = JsonDocument.Parse(jsonString);
                return jsonDoc.RootElement;
            }
        }

        public Members(string jsonString)
        {
            this.jsonString = jsonString;
        }

        public void Dispose()
        {
            if (this.jsonDoc != null)
                this.jsonDoc.Dispose();
        }
    }
}