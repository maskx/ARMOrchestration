using ARMCreatorTest;
using maskx.ARMOrchestration;
using maskx.ARMOrchestration.Orchestrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ARMOrchestrationTest
{
    [Collection("WebHost ARMOrchestartion")]
    [Trait("c", "ARMOrchestration")]
    public class ARMOrchestrationClientTest
    {
        private readonly ARMOrchestartionFixture _Fixture;
        private readonly ARMOrchestrationClient _Client;
        public ARMOrchestrationClientTest(ARMOrchestartionFixture fixture)
        {
            this._Fixture = fixture;
            this._Client = this._Fixture.ServiceProvider.GetService<ARMOrchestrationClient>();
        }
        [Fact(DisplayName = "DuplicateNameSameRequestId")]
        public void DuplicateNameSameRequestId()
        {
            string deployName = Guid.NewGuid().ToString("N");
            string deployId = Guid.NewGuid().ToString("N");
            string correlationId= Guid.NewGuid().ToString("N");
            string groupId= Guid.NewGuid().ToString("N");
            string content = TestHelper.GetTemplateContent("Condition/NoCondition");
            var t1 = this._Client.Run(new DeploymentOrchestrationInput()
            {
                TemplateContent = content,
                Parameters = string.Empty,
                CorrelationId = correlationId,
                DeploymentName = deployName,
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                GroupId = "1",
                GroupType = "ResourceGroup",
                HierarchyId = "001002003004005",
                CreateByUserId = TestHelper.CreateByUserId,
                ApiVersion = "1.0",
                TenantId = "000",
                DeploymentId = deployId
            });
            var t2 = this._Client.Run(new DeploymentOrchestrationInput()
            {
                TemplateContent = content,
                Parameters = string.Empty,
                CorrelationId = correlationId,
                DeploymentName = deployName,
                SubscriptionId = TestHelper.SubscriptionId,
                ResourceGroup = TestHelper.ResourceGroup,
                GroupId = "2",
                GroupType = "ResourceGroup",
                HierarchyId = "001002003004005",
                CreateByUserId = TestHelper.CreateByUserId,
                ApiVersion = "1.0",
                TenantId = "000",
                DeploymentId = Guid.NewGuid().ToString("N")
            });
            Task.WaitAll(t1, t2);

            var r1 = t1.Result;
            var r2 = t2.Result;
            Assert.Equal(r2.CorrelationId, correlationId);
            Assert.Equal(r1.ExecutionId, r2.ExecutionId);
            Assert.Equal(r1.InstanceId, r2.InstanceId);
            Assert.Equal(r1.CorrelationId, r2.CorrelationId);
            Assert.Equal(r1.DeploymentId, r2.DeploymentId);
            Assert.Equal(r1.ParentResourceId, $"{r2.InstanceId}:{r2.ExecutionId}");
            Assert.Equal(r2.ParentResourceId, $"{r1.InstanceId}:{r1.ExecutionId}");
        }
        [Fact(DisplayName = "DuplicateNameDifferentRequestId")]
        public void DuplicateNameDifferentRequestId()
        {
            string deployName = Guid.NewGuid().ToString("N");
            string deployId = Guid.NewGuid().ToString("N");
            string correlationId = Guid.NewGuid().ToString("N");
            string content = TestHelper.GetTemplateContent("Condition/NoCondition");
            Assert.Equal($"already have a deployment named {deployName}", Assert.ThrowsAny<AggregateException>(() =>
            {
                var t1 = this._Client.Run(new DeploymentOrchestrationInput()
                {
                    TemplateContent = content,
                    Parameters = string.Empty,
                    CorrelationId = correlationId,
                    DeploymentName = deployName,
                    SubscriptionId = TestHelper.SubscriptionId,
                    ResourceGroup = TestHelper.ResourceGroup,
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupType = "ResourceGroup",
                    HierarchyId = "001002003004005",
                    CreateByUserId = TestHelper.CreateByUserId,
                    ApiVersion = "1.0",
                    TenantId = "000",
                    DeploymentId = deployId
                });
               
                var t2 = this._Client.Run(new DeploymentOrchestrationInput()
                {
                    TemplateContent = content,
                    Parameters = string.Empty,
                    CorrelationId = Guid.NewGuid().ToString("N"),
                    DeploymentName = deployName,
                    SubscriptionId = TestHelper.SubscriptionId,
                    ResourceGroup = TestHelper.ResourceGroup,
                    GroupId = Guid.NewGuid().ToString("N"),
                    GroupType = "ResourceGroup",
                    HierarchyId = "001002003004005",
                    CreateByUserId = TestHelper.CreateByUserId,
                    ApiVersion = "1.0",
                    TenantId = "000",
                    DeploymentId = Guid.NewGuid().ToString("N")
                });
                Task.WaitAll(t1, t2);
            }).InnerException.Message);
        }
    }
}
