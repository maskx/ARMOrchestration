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
        #region Array and object

        [Trait("ARMFunctions", "Array and object")]
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

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "coalesce")]
        public void coalesce()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringOutput","\"default\"" },
                { "intOutput","1"},
                { "objectOutput","{\"first\": \"default\"}"},
                {"arrayOutput","[1]" },
                {"emptyOutput","true" }
            };
            TestHelper.FunctionTest("coalesce", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "concat")]
        public void concat()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"return",$"[{TestHelper.GetNodeStringValue("concat","parameters/firstArray/defaultValue")},{TestHelper.GetNodeStringValue("concat","parameters/secondArray/defaultValue")}]" }
            };
            TestHelper.FunctionTest("concat", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "contains")]
        public void contains()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringTrue",$"true" },
                {"stringFalse",$"false" },
                {"objectTrue",$"true" },
                {"objectFalse",$"false" },
                {"arrayTrue",$"true" },
                {"arrayFalse",$"false" }
            };
            TestHelper.FunctionTest("contains", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "createarray")]
        public void createarray()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringArray","[\"a\",\"b\",\"c\"]" },
                {"intArray","[1,2,3]" }
            };
            result.Add("objectArray", $"[{TestHelper.GetNodeStringValue("createarray", "parameters/objectToTest/defaultValue")}]");
            result.Add("arrayArray", $"[{TestHelper.GetNodeStringValue("createarray", "parameters/arrayToTest/defaultValue")}]");
            TestHelper.FunctionTest("createarray", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "empty")]
        public void empty()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayEmpty","true" },
                { "objectEmpty","true"},
                { "stringEmpty","true"}
            };
            TestHelper.FunctionTest("empty", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "first")]
        public void first()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayOutput","\"one\"" },
                { "stringOutput","\"O\""}
            };
            TestHelper.FunctionTest("first", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "intersection")]
        public void intersection()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"objectOutput","{\"one\":\"a\",\"three\":\"c\"}" },
                {"arrayOutput","[\"two\",\"three\"]"}
            };
            TestHelper.FunctionTest("intersection", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "json")]
        public void json()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"jsonOutput","{\"a\": \"b\"}" },
                {"nullOutput","true"},
                {"paramOutput","{\"a\": \"demo value\"}"}
            };
            TestHelper.FunctionTest("json", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "last")]
        public void last()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayOutput","\"three\"" },
                {"stringOutput","\"e\""}
            };
            TestHelper.FunctionTest("last", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "length")]
        public void length()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayLength","3" },
                {"stringLength","13"},
                {"objectLength","4" }
            };
            TestHelper.FunctionTest("length", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "max")]
        public void max()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayOutput","5" },
                {"intOutput","5"}
            };
            TestHelper.FunctionTest("max", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "min")]
        public void min()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayOutput","0" },
                {"intOutput","0"}
            };
            TestHelper.FunctionTest("min", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "range")]
        public void range()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"rangeOutput","[5,6,7]" }
            };
            TestHelper.FunctionTest("range", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "skip")]
        public void skip()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayOutput","[\"three\"]" },
                {"stringOutput","\"two three\"" }
            };
            TestHelper.FunctionTest("skip", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "take")]
        public void take()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayOutput","[\"one\",\"two\"]" },
                {"stringOutput","\"on\"" }
            };
            TestHelper.FunctionTest("take", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "union")]
        public void union()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"objectOutput","{\"one\":\"a\",\"two\":\"b\",\"three\":\"c2\",\"four\":\"d\",\"five\":\"e\"}" },
                {"arrayOutput","[\"one\",\"two\",\"three\",\"four\"]" }
            };
            TestHelper.FunctionTest("union", result);
        }

        #endregion Array and object

        #region Comparison

        [Trait("ARMFunctions", "Comparison")]
        [Fact(DisplayName = "equals")]
        public void equals()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"checkInts","true"},
                {"checkStrings","true" },
                {"checkArrays","true" },
                {"checkObjects","true"}
            };

            TestHelper.FunctionTest("equals", result);
        }

        [Trait("ARMFunctions", "Comparison")]
        [Fact(DisplayName = "greater")]
        public void greater()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"checkInts","false"},
                {"checkStrings","true" }
            };

            TestHelper.FunctionTest("greater", result);
        }

        [Trait("ARMFunctions", "Comparison")]
        [Fact(DisplayName = "greaterOrEquals")]
        public void greaterOrEquals()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"checkInts","false"},
                {"checkStrings","true" }
            };

            TestHelper.FunctionTest("greaterOrEquals", result);
        }

        [Trait("ARMFunctions", "Comparison")]
        [Fact(DisplayName = "less")]
        public void less()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"checkInts","true"},
                {"checkStrings","false" }
            };

            TestHelper.FunctionTest("less", result);
        }

        [Trait("ARMFunctions", "Comparison")]
        [Fact(DisplayName = "lessOrEquals")]
        public void lessOrEquals()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"checkInts","true"},
                {"checkStrings","false" }
            };

            TestHelper.FunctionTest("lessOrEquals", result);
        }

        #endregion Comparison

        #region Deployment

        [Trait("ARMFunctions", "Deployment")]
        [Fact(DisplayName = "parameters")]
        public void parameters()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringOutput","\"option 1\""},
                {"intOutput","1" },
                {"arrayOutput","[ 1, 2, 3 ]"},
                {"crossOutput","\"option 1\""}
            };
            result.Add("objectOutput", TestHelper.GetNodeStringValue("parameters", "parameters/objectParameter/defaultValue"));
            TestHelper.FunctionTest("parameters", result);
        }

        [Trait("ARMFunctions", "Deployment")]
        [Fact(DisplayName = "variables")]
        public void variables()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"exampleOutput1","\"myVariable\""},
                {"exampleOutput2","[ 1, 2, 3, 4 ]" },
                {"exampleOutput3","\"myVariable\""}
            };
            result.Add("exampleOutput4", TestHelper.GetNodeStringValue("variables", "variables/var4"));
            TestHelper.FunctionTest("variables", result);
        }

        #endregion Deployment
    }
}