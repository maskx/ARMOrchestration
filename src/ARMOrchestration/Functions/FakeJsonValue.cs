using System;
using System.Dynamic;

namespace maskx.ARMOrchestration.Functions
{
    public class FakeJsonValue : DynamicObject
    {
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = new FakeJsonValue();
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new FakeJsonValue();
            return true;
        }

        public override string ToString()
        {
            return Guid.NewGuid().ToString();
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