using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Functions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ARMOrchestrationTest.TestARMFunctions
{
    /// <summary>
    /// ARMFunctionsTest
    /// </summary>
    /// <seealso cref="https://github.com/Azure/azure-docs-json-samples/blob/master/azure-resource-manager/functions/"/>
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ARMFunctions")]
    public class ARMFunctionsTest
    {
        private readonly ARMOrchestartionFixture fixture;

        public ARMFunctionsTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact(DisplayName = "left bracket")]
        public void LeftBracket()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "Result","[test value]"}
            };
            TestHelper.FunctionTest(this.fixture, "LeftBracket", result);
        }

        [Fact(DisplayName = "escape double quotes")]
        public void EscapeDoubleQuotes()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "Result","{\"Dept\":\"Finance\",\"Environment\":\"Production\"}"}
            };
            TestHelper.FunctionTest(this.fixture, "EscapeDoubleQuotes", result);
        }

        #region Array and object

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "array")]
        public void Array()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "intOutput","[1]"},
                { "stringOutput","[\"efgh\"]"},
                { "objectOutput","[{\"a\":\"b\",\"c\":\"d\"}]"}
            };
            TestHelper.FunctionTest(this.fixture, "array", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "coalesce")]
        public void coalesce()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringOutput","default" },
                { "intOutput","1"},
                { "objectOutput","{\"first\":\"default\"}"},
                {"arrayOutput","[1]" },
                {"emptyOutput","true" }
            };
            TestHelper.FunctionTest(this.fixture, "coalesce", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "concat")]
        public void concat()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"return","[[\"1-1\",\"1-2\",\"1-3\"],[\"2-1\",\"2-2\",\"2-3\"]]"}
            };
            TestHelper.FunctionTest(this.fixture, "concat", result);
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
            TestHelper.FunctionTest(this.fixture, "contains", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "createarray")]
        public void createarray()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringArray","[\"a\",\"b\",\"c\"]"},
                {"intArray","[1,2,3]"},
                {"objectArray","[{\"one\":\"a\",\"two\":\"b\",\"three\":\"c\"}]"},
                {"arrayArray","[[\"one\",\"two\",\"three\"]]"}
            };
            TestHelper.FunctionTest(this.fixture, "createarray", result);
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
            TestHelper.FunctionTest(this.fixture, "empty", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "first")]
        public void first()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayOutput","one" },
                { "stringOutput","O"}
            };
            TestHelper.FunctionTest(this.fixture, "first", result);
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
            TestHelper.FunctionTest(this.fixture, "intersection", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "json")]
        public void json()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"jsonOutput","{\"a\":\"b\"}" },
                {"nullOutput","true"},
                {"paramOutput","{\"a\":\"demo value\"}"}
            };
            TestHelper.FunctionTest(this.fixture, "json", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "last")]
        public void last()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayOutput","three" },
                {"stringOutput","e"}
            };
            TestHelper.FunctionTest(this.fixture, "last", result);
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
            TestHelper.FunctionTest(this.fixture, "length", result);
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
            TestHelper.FunctionTest(this.fixture, "max", result);
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
            TestHelper.FunctionTest(this.fixture, "min", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "range")]
        public void range()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"rangeOutput","[5,6,7]" }
            };
            TestHelper.FunctionTest(this.fixture, "range", result);
        }

        [Trait("ARMFunctions", "String")]
        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "skip")]
        public void skip()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayOutput","[\"three\"]" },
                {"stringOutput","two three" }
            };
            TestHelper.FunctionTest(this.fixture, "skip", result);
        }

        [Trait("ARMFunctions", "String")]
        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "take")]
        public void take()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"arrayOutput","[\"one\",\"two\"]" },
                {"stringOutput","on" }
            };
            TestHelper.FunctionTest(this.fixture, "take", result);
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
            TestHelper.FunctionTest(this.fixture, "union", result);
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

            TestHelper.FunctionTest(this.fixture, "equals", result);
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

            TestHelper.FunctionTest(this.fixture, "greater", result);
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

            TestHelper.FunctionTest(this.fixture, "greaterOrEquals", result);
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

            TestHelper.FunctionTest(this.fixture, "less", result);
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

            TestHelper.FunctionTest(this.fixture, "lessOrEquals", result);
        }

        #endregion Comparison

        #region Deployment

        [Trait("ARMFunctions", "Deployment")]
        [Fact(DisplayName = "parameters")]
        public void parameters()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringOutput","option 1"},
                {"intOutput","1" },
                {"arrayOutput","[1,2,3]"},
                {"crossOutput","option 1"},
                {"objectOutput","{\"one\":\"a\",\"two\":\"b\"}"},
               {"arrayOutput1","2"},
            };
            //  result.Add("objectOutput", TestHelper.GetNodeStringValue("parameters", "parameters/objectParameter/defaultValue"));
            TestHelper.FunctionTest(this.fixture, "parameters", result);
        }

        [Trait("ARMFunctions", "Deployment")]
        [Fact(DisplayName = "variables")]
        public void variables()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"exampleOutput1","myVariable"},
                {"exampleOutput2","[1,2,3,4]" },
                {"exampleOutput3","myVariable"},
                {"exampleOutput4","{\"property1\":\"value1\",\"property2\":\"value2\"}"},
                { "newGuid","true"}
            };
            TestHelper.FunctionTest(this.fixture, "variables", result);
        }

        [Trait("ARMFunctions", "Deployment")]
        [Fact(DisplayName = "Deployment")]
        public void Deployment()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"deploymentOutput",TestHelper.GetJsonFileContent("TestARMFunctions/json/deployment") }
            };
            TestHelper.FunctionTest(this.fixture, "deployment", result);
        }

        #endregion Deployment

        #region Logical

        [Trait("ARMFunctions", "Logical")]
        [Fact(DisplayName = "and or not")]
        public void and()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"andExampleOutput","false"},
                {"orExampleOutput","true" },
                {"notExampleOutput","false"}
            };
            TestHelper.FunctionTest(this.fixture, "and", result);
        }

        [Trait("ARMFunctions", "Logical")]
        [Fact(DisplayName = "bool")]
        public void Bool()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"trueString","true"},
                {"falseString","false" },
                {"trueInt","true"},
                { "falseInt","false"}
            };
            TestHelper.FunctionTest(this.fixture, "bool", result);
        }

        [Trait("ARMFunctions", "Logical")]
        [Fact(DisplayName = "if")]
        public void If()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"yesOutput","yes"},
                {"noOutput","no" },
                {"objectOutput","{\"test\":\"value1\"}"}
            };
            TestHelper.FunctionTest(this.fixture, "if", result);
        }

        #endregion Logical

        #region Numeric

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "add")]
        public void add()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"addResult","8"}
            };
            TestHelper.FunctionTest(this.fixture, "add", result);
        }

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "copyindex")]
        public void copyindex()
        {
            Dictionary<string, object> cxt = new Dictionary<string, object>() {
                {"currentloopname","loop1" },
                {"copyindex",new Dictionary<string,int>(){
                    {"loop1",2 },
                    {"loop2",7 }
                }
                }
            };
            ARMFunctions functions = new ARMFunctions(
                Options.Create(new ARMOrchestrationOptions()),
                null,
                new MockInfrastructure(null));
            object rtv = functions.Evaluate("[copyindex()]", cxt);
            Assert.Equal(2, (int)rtv);
            rtv = functions.Evaluate("[copyindex(1)]", cxt);
            Assert.Equal(3, (int)rtv);
            rtv = functions.Evaluate("[copyindex('loop2')]", cxt);
            Assert.Equal(7, (int)rtv);
            rtv = functions.Evaluate("[copyindex('loop2',2)]", cxt);
            Assert.Equal(9, (int)rtv);
        }

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "div")]
        public void div()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"divResult","2"}
            };
            TestHelper.FunctionTest(this.fixture, "div", result);
        }

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "int")]
        public void Int()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"intResult","4"}
            };
            TestHelper.FunctionTest(this.fixture, "int", result);
        }

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "mod")]
        public void mod()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"modResult","1"}
            };
            TestHelper.FunctionTest(this.fixture, "mod", result);
        }

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "mul")]
        public void mul()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"mulResult","15"}
            };
            TestHelper.FunctionTest(this.fixture, "mul", result);
        }

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "sub")]
        public void sub()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"subResult","4"}
            };
            TestHelper.FunctionTest(this.fixture, "sub", result);
        }

        #endregion Numeric

        #region String

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "base64")]
        public void base64()
        {
            //base64tojson: the JSON standard need double quota
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"base64Output","b25lLCB0d28sIHRocmVl"},
                {"toStringOutput","one, two, three"},
                {"toJsonOutput","{\"one\":\"a\",\"two\":\"b\"}"}
            };
            TestHelper.FunctionTest(this.fixture, "base64", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "dataUri")]
        public void dataUri()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"dataUriOutput","data:text/plain;charset=utf8;base64,SGVsbG8="},
                {"toStringOutput","Hello, World!"}
            };
            TestHelper.FunctionTest(this.fixture, "dataUri", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "endsWith startsWith")]
        public void endsWith()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"startsTrue","true"},
                {"startsCapTrue","true"},
                {"startsFalse","false"},
                {"endsTrue","true"},
                {"endsCapTrue","true"},
                {"endsFalse","false"}
            };
            TestHelper.FunctionTest(this.fixture, "endsWith", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "format")]
        public void format()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"formatTest","Hello, User. Formatted number: 8,175,133" }
            };
            TestHelper.FunctionTest(this.fixture, "format", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "guid")]
        public void guid()
        {
            var func = this.fixture.ServiceProvider.GetService<ARMFunctions>();
            var abc = func.Evaluate("[guid('abcdefghijklmn')]", null).ToString();
            var xyz = func.Evaluate("[guid('xyz','opqrst','uvw1','123','4564')]", null).ToString();
            var abc1 = func.Evaluate("[guid('abcdefghijklmn')]", null).ToString();
            var xyz2 = func.Evaluate("[guid('xyz','opqrst','uvw1','123','4564')]", null).ToString();
            Assert.Equal("b74a9217-f91d-54f5-86a8-bd998e2d611d", abc);
            Assert.Equal("51e07a2f-6c47-2383-a3ea-376e5bd2df27", xyz);
            Assert.Equal("b74a9217-f91d-54f5-86a8-bd998e2d611d", abc1);
            Assert.Equal("51e07a2f-6c47-2383-a3ea-376e5bd2df27", xyz2);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "uniqueString")]
        public void uniqueString()
        {
            var func = this.fixture.ServiceProvider.GetService<ARMFunctions>();
            var abc = func.Evaluate("[uniqueString('abcdefghijklmn')]", null).ToString();
            var xyz = func.Evaluate("[uniqueString('xyz','opqrst','uvw1','123','4564')]", null).ToString();
            var abc1 = func.Evaluate("[uniqueString('abcdefghijklmn')]", null).ToString();
            var xyz2 = func.Evaluate("[uniqueString('xyz','opqrst','uvw1','123','4564')]", null).ToString();
            Assert.Equal("4tmzhlnssjckt", abc);
            Assert.Equal("k6gotveobkoyh", xyz);
            Assert.Equal("4tmzhlnssjckt", abc1);
            Assert.Equal("k6gotveobkoyh", xyz2);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "newGuid")]
        public void newGuid()
        {
            string filename = "newGuid";
            var templateString = TestHelper.GetFunctionInputContent(filename);
            var instance = fixture.OrchestrationWorker.JumpStartOrchestrationAsync(new Job()
            {
                InstanceId = Guid.NewGuid().ToString("N"),
                Orchestration = new OrchestrationSetting()
                {
                    Name = "DeploymentOrchestration",
                    Version = "1.0"
                },
                Input = TestHelper.DataConverter.Serialize(new DeploymentOrchestrationInput()
                {
                    Template = templateString,
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    DeploymentName = filename.Replace('/', '-'),
                    SubscriptionId = TestHelper.SubscriptionId,
                    ResourceGroup = TestHelper.ResourceGroup,
                    DeploymentId = Guid.NewGuid().ToString("N"),
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupType = "ResourceGroup",
                    HierarchyId = "001002003004005",
                    CreateByUserId = TestHelper.CreateByUserId,
                })
            }).Result;
            TaskCompletionSource<string> t = new TaskCompletionSource<string>();

            fixture.OrchestrationWorker.RegistOrchestrationCompletedAction((args) =>
            {
                if (!args.IsSubOrchestration && args.InstanceId == instance.InstanceId)
                    t.SetResult(args.Result);
            });
            var outputString = TestHelper.DataConverter.Deserialize<TaskResult>(t.Task.Result).Content.ToString();

            using var outputDoc = JsonDocument.Parse(outputString);
            var outputRoot = outputDoc.RootElement.GetProperty("properties").GetProperty("outputs");

            Assert.True(outputRoot.TryGetProperty("guidOutput", out JsonElement v), $"cannot find guidOutput in output");
            Assert.True(Guid.TryParse(v.GetProperty("value").GetString(), out Guid d));
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "indexOf lastIndexOf")]
        public void indexOf()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"firstT","0" },
                {"lastT","3" },
                {"firstString","2" },
                {"lastString","0" },
                {"notFound","-1" },
            };
            TestHelper.FunctionTest(this.fixture, "indexOf", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "padLeft")]
        public void padLeft()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringOutput","0000000123" }
            };
            TestHelper.FunctionTest(this.fixture, "padLeft", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "replace")]
        public void replace()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"firstOutput","1231231234" },
                {"secondOutput","123-123-xxxx" }
            };
            TestHelper.FunctionTest(this.fixture, "replace", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "split")]
        public void split()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"firstOutput","[\"one\",\"two\",\"three\"]"},
                {"secondOutput","[\"one\",\"two\",\"three\"]"}
            };
            TestHelper.FunctionTest(this.fixture, "split", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "string")]
        public void stringMethod()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "intOutput","5"}
            };
            result.Add("objectOutput", TestHelper.GetNodeStringValue("string", "parameters/testObject/defaultValue"));
            result.Add("arrayOutput", TestHelper.GetNodeStringValue("string", "parameters/testArray/defaultValue"));
            TestHelper.FunctionTest(this.fixture, "string", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "substring")]
        public void substring()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "substringOutput","two"}
            };
            TestHelper.FunctionTest(this.fixture, "substring", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "toLower toUpper")]
        public void toLower()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "toLowerOutput","one two three"},
                { "toUpperOutput","ONE TWO THREE"}
        };
            TestHelper.FunctionTest(this.fixture, "toLower", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "trim")]
        public void trim()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "return","one two three"}
        };
            TestHelper.FunctionTest(this.fixture, "trim", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "uri uriComponentToString uriComponent")]
        public void uri()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "uriOutput","http://contoso.com/resources/nested/azuredeploy.json"},
                { "componentOutput","http%3A%2F%2Fcontoso.com%2Fresources%2Fnested%2Fazuredeploy.json"},
                { "toStringOutput","http://contoso.com/resources/nested/azuredeploy.json"}
            };
            TestHelper.FunctionTest(this.fixture, "uri", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "utcNow")]
        public void utcNow()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "utcOutput",DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'")},
                { "utcShortOutput",DateTime.UtcNow.ToString("d")},
                { "utcCustomOutput",DateTime.UtcNow.ToString("M d")}
            };
            TestHelper.FunctionTest(this.fixture, "utcNow", result);
        }

        #endregion String

        [Trait("ARMFunctions", "JsonValue")]
        [Fact(DisplayName = "Indexer")]
        public void Indexer()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"indexer_indexer","a"},
                { "indexer_Memberindexer","a"}
            };
            TestHelper.FunctionTest(this.fixture, "JsonValue/JsonValue", result);
        }
    }
}