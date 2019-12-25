using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ARMCreatorTest
{
    [Trait("C", "ARMFunctions")]
    public class UserDefinedFunctionTest
    {
        [Trait("ARMFunctions", "UserDefinedFunction")]
        [Fact(DisplayName = "UserDefinedFunction")]
        public void UserDefinedFunction()
        {
            Dictionary<string, string> result = new Dictionary<string, string>()
            {
                {"addResult","abc-123"}
            };
            TestHelper.FunctionTest("UserDefinedFunction", result);
        }
    }
}