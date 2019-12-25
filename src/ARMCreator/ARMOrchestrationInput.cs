using maskx.OrchestrationCreator.ARMTemplate;
using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.OrchestrationCreator
{
    public class ARMOrchestrationInput
    {
        public Template Template { get; set; }
        public string Parameters { get; set; }
    }
}