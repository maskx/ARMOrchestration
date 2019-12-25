using System;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.OrchestrationCreator.ARMTemplate
{
    public class Template
    {
        public string Schema { get; set; }
        public string ContentVersion { get; set; }
        public string ApiProfile { get; set; }
        public string Parameters { get; set; }
        public string Variables { get; set; }
        public List<Resource> Resources { get; set; } = new List<Resource>();

        public Dictionary<string, Function> Functions { get; set; } = new Dictionary<string, Function>();
        public string Outputs { get; set; }

        public static Template Parse(string str)
        {
            Template template = new Template();
            using var jsonDoc = JsonDocument.Parse(str);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("contentVersion", out JsonElement contentVersionEle))
            {
                template.ContentVersion = contentVersionEle.GetString();
            }
            if (root.TryGetProperty("apiProfile", out JsonElement apiProfileEle))
            {
                template.ApiProfile = apiProfileEle.GetString();
            }
            if (root.TryGetProperty("functions", out JsonElement functionsEle))
            {
                foreach (var funcDef in functionsEle.EnumerateArray())
                {
                    if (!funcDef.TryGetProperty("namespace", out JsonElement nsEle))
                    {
                        throw new Exception("cannot find namespace in functions element");
                    }
                    string ns = nsEle.GetString();
                    if (string.IsNullOrEmpty(ns))
                    {
                        throw new Exception("namespace cannot be empty");
                    }
                    if (funcDef.TryGetProperty("members", out JsonElement memebersEle))
                    {
                        foreach (var m in memebersEle.EnumerateObject())
                        {
                            var func = new Function() { FullName = $"{ns}.{m.Name}".ToLower() };
                            if (!m.Value.TryGetProperty("output", out JsonElement outputEle))
                            {
                                throw new Exception($"cannot find output member in functions/members/{m.Name}");
                            }
                            func.Output = outputEle.GetRawText();
                            if (m.Value.TryGetProperty("parameters", out JsonElement parEle))
                            {
                                func.Parameters = parEle.GetRawText();
                            }
                            template.Functions.Add(func.FullName, func);
                        }
                    }
                }
            }
            if (root.TryGetProperty("parameters", out JsonElement parameterDefineElement))
            {
                template.Parameters = parameterDefineElement.GetRawText();
            }
            if (root.TryGetProperty("variables", out JsonElement variableDefineElement))
            {
                template.Variables = variableDefineElement.GetRawText();
            }
            if (root.TryGetProperty("resources", out JsonElement resources))
            {
                foreach (var item in resources.EnumerateArray())
                {
                    template.Resources.Add(Resource.Parse(item.GetRawText()));
                }
            }
            if (root.TryGetProperty("outputs", out JsonElement outputDefineElement))
            {
                template.Outputs = outputDefineElement.GetRawText();
            }
            return template;
        }
    }
}