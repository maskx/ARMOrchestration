using System.Collections.Generic;
using Xunit;

namespace ARMCreatorTest.TestARMFunctions
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("C", "ARMFunctions")]
    public class UserDefinedFunctionTest
    {
        private ARMOrchestartionFixture fixture;

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
            TestHelper.FunctionTest(this.fixture.OrchestrationWorker, "UserDefinedFunction", result);
        }
    }
}