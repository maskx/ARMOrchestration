using System;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class Functions : IDisposable
    {
        public Members this[string ns]
        {
            get
            {
                foreach (var funcDef in root.EnumerateArray())
                {
                    if (funcDef.TryGetProperty("namespace", out JsonElement nsEle) && nsEle.GetString() == ns)
                    {
                        return new Members(funcDef.GetProperty("members").GetRawText());
                    }
                }

                return null;
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
        public string Parameters
        {
            get
            {
                return root.GetProperty("parameters").GetRawText();
            }
        }

        public string Output
        {
            get
            {
                return root.GetProperty("output").GetRawText();
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