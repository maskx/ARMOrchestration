using ARMOrchestrationTest;
using ARMOrchestrationTest.Mock;
using maskx.ARMOrchestration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ARMOrchestrationTest
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ARMOrchestration")]
    public class ARMOrchestrationClientTest
    {
        private readonly ARMOrchestartionFixture _Fixture;
        private readonly ARMOrchestrationClient<CustomCommunicationJob> _Client;
        public ARMOrchestrationClientTest(ARMOrchestartionFixture fixture)
        {
            this._Fixture = fixture;
            this._Client = this._Fixture.ServiceProvider.GetService<ARMOrchestrationClient<CustomCommunicationJob>>();
        }

    }
}
