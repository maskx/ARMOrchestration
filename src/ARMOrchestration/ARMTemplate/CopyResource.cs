using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using System;
using System.Text.Json;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class CopyResource : Resource
    {
        public const string ServiceType = "copy";
        public const string SerialMode = "serial";
        public const string ParallelMode = "parallel";

        public CopyResource(JsonElement element, DeploymentOrchestrationInput input, string parentName = null, string parentType = null)
            : base(element, input, parentName, parentType)
        {
        }

        private int? _Count;

        public int Count
        {
            get
            {
                if (!_Count.HasValue)
                {
                    if (!RootElement.TryGetProperty("count", out JsonElement count))
                        throw new Exception("not find count in copy node");
                    if (count.ValueKind == JsonValueKind.Number)
                        _Count = count.GetInt32();
                    else if (count.ValueKind == JsonValueKind.String)
                    {
                        // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#valid-uses-1
                        if (Context.ContainsKey(ContextKeys.DEPENDSON))
                            throw new Exception("You can't use the reference function to set the value of the count property in a copy loop.");
                        _Count = (int)_Functions.Evaluate(count.GetString(), Context);
                    }
                    else
                        throw new Exception("the value of count property should be Number in copy node");
                }
                return _Count.Value;
            }
        }

        /// <summary>
        /// "serial" <or> "parallel"
        /// </summary>
        public string Mode
        {
            get
            {
                if (RootElement.TryGetProperty("mode", out JsonElement mode))
                {
                    return mode.GetString().ToLower();
                }
                return ParallelMode;
            }
        }

        private int? _BatchSize;

        /// <summary>
        /// number-to-deploy-serially
        /// </summary>
        public int BatchSize
        {
            get
            {
                if (!_BatchSize.HasValue)
                {
                    if (RootElement.TryGetProperty("batchSize", out JsonElement batchSize))
                    {
                        if (batchSize.ValueKind == JsonValueKind.Number)
                            _BatchSize = batchSize.GetInt32();
                        else if (batchSize.ValueKind == JsonValueKind.String)
                            _BatchSize = (int)_Functions.Evaluate(batchSize.GetString(), Context);
                    }
                }
                return _BatchSize.Value;
            }
        }

        public ResourceCollection Resources { get; set; }
    }
}