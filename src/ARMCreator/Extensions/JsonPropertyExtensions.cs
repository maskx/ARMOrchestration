﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace maskx.OrchestrationCreator.Extensions
{
    public class JsonPropertyEqualityComparer : IEqualityComparer<JsonProperty>
    {
        public bool Equals(JsonProperty x, JsonProperty y)
        {
            if (x.Name != y.Name)
                return false;
            return x.Value.IsEqual(y.Value);
        }

        public int GetHashCode(JsonProperty obj)
        {
            throw new NotImplementedException();
        }
    }

    public static class JsonPropertyExtensions
    {
    }
}