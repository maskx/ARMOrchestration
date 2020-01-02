﻿using maskx.OrchestrationCreator.Orchestrations;
using System.Collections.Generic;
using System.Text.Json;

namespace maskx.OrchestrationCreator.ARMTemplate
{
    /// <summary>
    /// https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/create-multiple-instances
    /// </summary>
    public class Copy
    {
        /// <summary>
        /// name-of-loop
        /// </summary>
        public string Name
        {
            get
            {
                if (root.TryGetProperty("name", out JsonElement name))
                {
                    return name.GetString();
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// number-of-iterations
        /// </summary>
        public int Count
        {
            get
            {
                if (root.TryGetProperty("count", out JsonElement count))
                {
                    return (int)ARMFunctions.Evaluate(count.GetString(), this.context);
                }
                return 0;
            }
        }

        /// <summary>
        /// "serial" <or> "parallel"
        /// </summary>
        public string Mode
        {
            get
            {
                if (root.TryGetProperty("mode", out JsonElement mode))
                {
                    return mode.GetString();
                }
                return "parallel";
            }
        }

        /// <summary>
        /// number-to-deploy-serially
        /// </summary>
        public int BatchSize
        {
            get
            {
                if (root.TryGetProperty("batchSize", out JsonElement batchSize))
                {
                    if (batchSize.ValueKind == JsonValueKind.Number)
                        return batchSize.GetInt32();
                    else if (batchSize.ValueKind == JsonValueKind.String)
                        return (int)ARMFunctions.Evaluate(batchSize.GetString(), this.context);
                }
                return 1;
            }
        }

        /// <summary>
        /// values-for-the-property-or-variable
        /// </summary>
        public string Input
        {
            get
            {
                if (root.TryGetProperty("input", out JsonElement input))
                {
                    return input.GetString();
                }
                return string.Empty;
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

        private ARMOrchestrationInput armInput;

        public override string ToString()
        {
            return this.jsonString;
        }

        public void Dispose()
        {
            if (this.jsonDoc != null)
            {
                jsonDoc.Dispose();
            }
        }

        private Dictionary<string, object> context;

        public Copy(string jsonString, Dictionary<string, object> context)
        {
            this.jsonString = jsonString;
            this.context = context;
            this.armInput = context["armcontext"] as ARMOrchestrationInput;
        }
    }
}