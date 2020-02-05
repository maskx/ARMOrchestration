using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Orchestrations;
using maskx.OrchestrationService;
using maskx.OrchestrationService.Worker;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ARMCreatorTest.TestARMFunctions
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ARMFunctions")]
    public class ARMFunctionsTest
    {
        private ARMOrchestartionFixture fixture;

        public ARMFunctionsTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        #region Array and object

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "array")]
        public void array()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "intOutput","[1]"},
                { "stringOutput","[\"efgh\"]"},
                { "objectOutput","[{\"a\":\"b\",\"c\":\"d\"}]"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "array", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "coalesce", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "concat")]
        public void concat()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"return","[[\"1-1\",\"1-2\",\"1-3\"],[\"2-1\",\"2-2\",\"2-3\"]]"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "concat", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "contains", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "createarray", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "empty", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "first", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "intersection", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "json", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "last", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "length", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "max", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "min", result);
        }

        [Trait("ARMFunctions", "Array and object")]
        [Fact(DisplayName = "range")]
        public void range()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"rangeOutput","[5,6,7]" }
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "range", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "skip", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "take", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "union", result);
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

            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "equals", result);
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

            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "greater", result);
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

            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "greaterOrEquals", result);
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

            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "less", result);
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

            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "lessOrEquals", result);
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
                {"objectOutput","{\"one\":\"a\",\"two\":\"b\"}"}
            };
            //  result.Add("objectOutput", TestHelper.GetNodeStringValue("parameters", "parameters/objectParameter/defaultValue"));
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "parameters", result);
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
                {"exampleOutput4","{\"property1\":\"value1\",\"property2\":\"value2\"}"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "variables", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "and", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "bool", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "if", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "add", result);
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
                new MockInfrastructure());
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "div", result);
        }

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "int")]
        public void Int()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"intResult","4"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "int", result);
        }

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "mod")]
        public void mod()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"modResult","1"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "mod", result);
        }

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "mul")]
        public void mul()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"mulResult","15"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "mul", result);
        }

        [Trait("ARMFunctions", "Numeric")]
        [Fact(DisplayName = "sub")]
        public void sub()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"subResult","4"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "sub", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "base64", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "dataUri", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "endsWith", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "format")]
        public void format()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"formatTest","Hello, User. Formatted number: 8,175,133" }
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "format", result);
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
                    Creator = "DICreator",
                    Uri = typeof(DeploymentOrchestration).FullName + "_"
                },
                Input = TestHelper.DataConverter.Serialize(new DeploymentOrchestrationInput()
                {
                    Template = templateString,
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    Name = filename.Replace('/', '-'),
                    SubscriptionId = TestHelper.SubscriptionId,
                    ResourceGroup = TestHelper.ResourceGroup
                })
            }).Result;
            TaskCompletionSource<string> t = new TaskCompletionSource<string>();

            fixture.OrchestrationWorker.RegistOrchestrationCompletedAction((args) =>
            {
                if (!args.IsSubOrchestration && args.InstanceId == instance.InstanceId)
                    t.SetResult(args.Result);
            });
            var outputString = TestHelper.DataConverter.Deserialize<TaskResult>(t.Task.Result).Content;

            using var outputDoc = JsonDocument.Parse(outputString);
            var outputRoot = outputDoc.RootElement;

            Assert.True(outputRoot.TryGetProperty("guidOutput", out JsonElement v), $"cannot find guidOutput in output");
            Assert.True(Guid.TryParse(v.GetString(), out Guid d));
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "indexOf", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "padLeft")]
        public void padLeft()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"stringOutput","0000000123" }
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "padLeft", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "replace", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "split", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "string", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "substring")]
        public void substring()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "substringOutput","two"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "substring", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "toLower", result);
        }

        [Trait("ARMFunctions", "String")]
        [Fact(DisplayName = "trim")]
        public void trim()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                { "return","one two three"}
        };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "trim", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "uri", result);
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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "utcNow", result);
        }

        #endregion String

        #region Resource

        [Trait("ARMFunctions", "Resource")]
        [Fact(DisplayName = "extensionResourceId")]
        public void extensionResourceId()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"lockResourceId",$"/subscriptions/{TestHelper.SubscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.Authorization/locks/lockname1/"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "extensionResourceId", result);
        }

        [Trait("ARMFunctions", "Resource")]
        [Fact(DisplayName = "resourceid")]
        public void resourceid()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"sameRGOutput",$"/subscriptions/{TestHelper.SubscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                {"differentRGOutput",$"/subscriptions/{TestHelper.SubscriptionId}/resourceGroups/otherResourceGroup/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                {"differentSubOutput","/subscriptions/11111111-1111-1111-1111-111111111111/resourceGroups/otherResourceGroup/providers/Microsoft.Storage/storageAccounts/examplestorage"},
                { "nestedResourceOutput",$"/subscriptions/{TestHelper.SubscriptionId}/resourceGroups/{TestHelper.ResourceGroup}/providers/Microsoft.SQL/servers/serverName/databases/databaseName"}
            };
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "resourceid", result);
        }

        [Trait("ARMFunctions", "Resource")]
        [Fact(DisplayName = "list*")]
        public void ListResource()
        {
            ARMFunctions functions = new ARMFunctions(
                Options.Create(new ARMOrchestrationOptions()),
                null,
                new MockInfrastructure());
            object rtv = functions.Evaluate(
                "[listResource('resourceId','2019-01-02')]",
                new Dictionary<string, object>() {
                    {"armcontext",new DeploymentContext(){
                        Template=new Template() } }
                });
            Assert.Equal("Resource", rtv.ToString());
        }

        #endregion Resource
    }
}