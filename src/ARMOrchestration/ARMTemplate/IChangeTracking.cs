using System.Runtime.CompilerServices;

namespace maskx.ARMOrchestration.ARMTemplate
{
    public interface IChangeTracking
    {
        long TrackingVersion { get; set; }

        bool Accepet(long newVersion = 0);

        void Change(object value, [CallerMemberName] string name = "");

        bool HasChanged { get; }
    }
}