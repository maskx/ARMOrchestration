using System.Collections.Generic;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public class Parameter
    {
        public string Type { get; set; }
        public object DefaultValue { get; set; }
        public List<object> AllowValues { get; set; }
        public object MinValue { get; set; }
        public object MinLength { get; set; }
        public object MaxValue { get; set; }
        public object MaxLength { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}