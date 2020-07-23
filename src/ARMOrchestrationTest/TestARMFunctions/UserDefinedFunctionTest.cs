using System.Collections.Generic;
using Xunit;

namespace ARMOrchestrationTest.TestARMFunctions
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ARMFunctions")]
    public class UserDefinedFunctionTest
    {
        private readonly ARMOrchestartionFixture fixture;

        public UserDefinedFunctionTest(ARMOrchestartionFixture fixture)
        {
            this.fixture = fixture;
        }

        [Trait("ARMFunctions", "UserDefinedFunction")]
        [Fact(DisplayName = "UserDefinedFunction")]
        public void UserDefinedFunction()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"addResult","abc-123"}
            };
            TestHelper.FunctionTest(this.fixture, "UserDefinedFunction", result);
        }
    }
}