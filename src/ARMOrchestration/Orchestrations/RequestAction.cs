using System;
using System.Collections.Generic;
using System.Text;

namespace maskx.ARMOrchestration.Orchestrations
{
    public enum RequestAction
    {
        CheckPermission,
        CheckLock,
        CheckPolicy,
        CheckResource,
        CheckQuota,
        CreateResource,
        UpdateResource,
        CreateExtensionResource,
        CommitQuota,
        CommitResource,
        ApplyPolicy,
        ReadyResource
    }
}