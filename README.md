## About Azure Resource Manager
https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/

## Quick start

1. Using ARMOrchestrationService

``` CSharp
 var sqlConfig = new ARMOrchestrationSqlServerConfig()
    {
        Database = new DatabaseConfig()
        {
            ConnectionString = TestHelper.ConnectionString,
            AutoCreate = true
        }
    };
 services.UsingARMOrchestration(sqlConfig);
```

2. Add On-premises support

for more information about IInfrastructure, please reference IInfrastructure

```CSharp
 services.AddSingleton<IInfrastructure>((sp) =>
 {
     return new MockInfrastructure(sp);
 });
```

3. Add Communication Processsor

for more information about Communication Processor, please reference https://github.com/maskx/OrchestrationService#icommunicationprocessor 

```CSharp
 services.AddSingleton<ICommunicationProcessor>((sp) =>
 {
     return new MockCommunicationProcessor();
 });
```

4. Provisioning resource by ARM Templates

``` CSharp
 var instance = webHost.Services.GetService<ARMOrchestrationClient>().Run(
    new DeploymentOrchestrationInput()
    {
        ApiVersion = "1.0",
        DeploymentName = "UsingARMOrchestrationTest",
        DeploymentId = Guid.NewGuid().ToString("N"),
        TemplateContent = TestHelper.GetTemplateContent("dependsOn/OneResourceName"),
        SubscriptionId = TestHelper.SubscriptionId,
        ResourceGroup = TestHelper.ResourceGroup,
        CorrelationId = Guid.NewGuid().ToString("N"),
        GroupId = Guid.NewGuid().ToString("N"),
        GroupType = "ResourceGroup",
        HierarchyId = "001002003004005",
        TenantId = "TenantId"
    }).Result;
```


## IInfrastructure

### Property

### Method

## DeploymentOrchestrationInput


