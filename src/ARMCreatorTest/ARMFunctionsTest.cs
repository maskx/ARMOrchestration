using maskx.OrchestrationCreator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Xunit;

namespace ARMCreatorTest
{
    [Trait("C", "ARMFunctions")]
    public class ARMFunctionsTest
    {
        [Fact(DisplayName = "array")]
        public void array()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "intOutput","[1]"},
                { "stringOutput","[\"efgh\"]"}
            };
            result.Add("objectOutput", $"[{TestHelper.GetNodeStringValue("array", "parameters/objectToConvert/defaultValue")}]");
            TestHelper.FunctionTest("array", result);
        }

        [Fact(DisplayName = "coalesce")]
        public void coalesce()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringOutput","\"default\"" },
                { "intOutput","1"},
                { "objectOutput","{\"first\": \"default\"}"},
                {"arrayOutput","[1]" },
                {"emptyOutput","\"True\"" }
            };
            TestHelper.FunctionTest("coalesce", result);
        }

        [Fact(DisplayName = "empty")]
        public void empty()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayEmpty","\"True\"" },
                { "objectEmpty","\"True\""},
                { "stringEmpty","\"True\""}
            };
            TestHelper.FunctionTest("empty", result);
        }
        [Fact(DisplayName = "concat")]
        public void concat()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"return",$"[{TestHelper.GetNodeStringValue("concat","parameters/firstArray/defaultValue")},{TestHelper.GetNodeStringValue("concat","parameters/secondArray/defaultValue")}]" }
            };
            TestHelper.FunctionTest("concat", result);
        }
        [Fact(DisplayName = "contains")]
        public void contains()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringTrue",$"\"True\"" },
                {"stringFalse",$"\"False\"" },
                {"objectTrue",$"\"True\"" },
                {"objectFalse",$"\"False\"" },
                {"arrayTrue",$"\"True\"" },
                {"arrayFalse",$"\"False\"" }
            };
            TestHelper.FunctionTest("contains", result);
        }
    }
}