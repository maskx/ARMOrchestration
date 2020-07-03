﻿using maskx.ARMOrchestration.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class Template
    {
        public string Schema { get; set; }

        public string ContentVersion { get; set; }

        public string ApiProfile { get; set; }

        public string Parameters { get; set; }

        public string Variables { get; set; }

        public Dictionary<string, Resource> Resources { get; set; } = new Dictionary<string, Resource>();

        public Functions Functions { get; set; }

        public string Outputs { get; set; }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-to-subscription
        /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/deploy-to-management-group
        /// </summary>
        public DeployLevel DeployLevel { get; set; }

        internal List<string> ConditionFalseResources { get; private set; } = new List<string>();

        public static Template Parse(string content)
        {
            Template template = new Template();
            using JsonDocument doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("$schema", out JsonElement schema))
                template.Schema = schema.GetString();
            if (root.TryGetProperty("contentVersion", out JsonElement contentVersion))
                template.ContentVersion = contentVersion.GetString();
            if (template.Schema.EndsWith("deploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                template.DeployLevel = DeployLevel.ResourceGroup;
            else if (template.Schema.EndsWith("subscriptionDeploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                template.DeployLevel = DeployLevel.Subscription;
            else if (template.Schema.EndsWith("managementGroupDeploymentTemplate.json#", StringComparison.InvariantCultureIgnoreCase))
                template.DeployLevel = DeployLevel.ManagemnetGroup;
            if (root.TryGetProperty("apiProfile", out JsonElement apiProfile))
                template.ApiProfile = apiProfile.GetString();
            if (root.TryGetProperty("parameters", out JsonElement parameters))
                template.Parameters = parameters.GetRawText();
            if (root.TryGetProperty("outputs", out JsonElement outputs))
                template.Outputs = outputs.GetRawText();
            return template;
        }

        public override string ToString()
        {
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
            writer.WriteStartObject();

            writer.WriteString("$schema", this.Schema);
            writer.WriteString("contentVersion", this.ContentVersion);
            if (!string.IsNullOrEmpty(this.ApiProfile))
                writer.WriteString("apiProfile", this.ApiProfile);
            if (!string.IsNullOrEmpty(this.Parameters))
                writer.WritRawString("parameters", this.Parameters);
            if (!string.IsNullOrEmpty(this.Variables))
                writer.WritRawString("variables", this.Variables);
            if (!string.IsNullOrEmpty(this.Outputs))
                writer.WritRawString("outputs", this.Outputs);
            writer.WritePropertyName("resources");
            writer.WriteStartArray();
            foreach (var r in Resources.Values)
            {
                writer.WriteRawString(r.ToString());
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();
            return Encoding.UTF8.GetString(ms.ToArray());

        }
    }
}