using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ARMCreatorTest
{
    public class JsonValueTest
    {
        [Fact]
        public void dd()
        {
            JsonDocument doc1 = JsonDocument.Parse("{\"a\":{\"b\":1}}");
            JsonDocument doc2 = JsonDocument.Parse("{\"a\": 1}");
            var d = doc1.RootElement.GetProperty("b");
            if (doc1.RootElement.GetProperty("a").ValueEquals(doc2.RootElement.GetRawText()))
            {
                var b = 1;
            }
        }
    }
}