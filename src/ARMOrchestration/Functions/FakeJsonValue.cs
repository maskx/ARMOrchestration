using System;
using System.Dynamic;

namespace maskx.ARMOrchestration.Functions
{
    public class FakeJsonValue : DynamicObject
    {
        private readonly string _Content;
        public FakeJsonValue()
        {
            _Content= Guid.NewGuid().ToString();
        }
        public FakeJsonValue(string content)
        {
            _Content = content;
        }
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = new FakeJsonValue(_Content);
            return true;
        }
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new FakeJsonValue(_Content);
            return true;
        }

        public override string ToString()
        {
            return _Content;
        }

        public static implicit operator Int32(FakeJsonValue f)
        {
            return 1;
        }

        public static implicit operator Int64(FakeJsonValue f)
        {
            return 1;
        }
    }
}