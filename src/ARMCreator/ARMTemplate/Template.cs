using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationCreator.ARMTemplate
{
    public class Template
    {
        public string Schema { get; set; }
        public string contentVersion { get; set; }
        public string ApiProfile { get; set; }
        public Dictionary<string, Parameter> Parameters { get; set; }
        public Dictionary<string, Variable> Variables { get; set; }
        public List<Resource> Resources { get; set; }
        public List<Function> Functions { get; set; }
        public Dictionary<string, Output> Outputs { get; set; }
    }
}