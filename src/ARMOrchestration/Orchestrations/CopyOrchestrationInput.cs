using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.Orchestrations
{
    public class CopyOrchestrationInput : ResourceOrchestrationInput
    {
        public List<Resource> Resources { get; set; } = new List<Resource>();
    }
}