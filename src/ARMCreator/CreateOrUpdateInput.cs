using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationCreator
{
    public class CreateOrUpdateInput
    {
        public string Resource { get; set; }
        public string Parameters { get; set; }
        public string Variable { get; set; }
        public string ParameterDefine { get; set; }
    }
}