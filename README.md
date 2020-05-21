
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

|Name|Description|
|-|-|
|ApiVersion||
|ApiVersion||
|ApiVersion||
|ApiVersion||
|ApiVersion||
|ApiVersion||
|ApiVersion||
|ApiVersion||
|ApiVersion||

TODO:
* WhatIf suppport
* [uniqueString](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-string#uniquestring)


## [ARM Functions](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions)

```CSharp
ARMFunctions.Evaluate(string function, Dictionary<string, object> context)
```
The context need 1 value:

|key|value|description|
|-|-|-|
|armcontext|DeploymentContext||
|copyindex|Dictionary<string,int>|the key is loopname,and the value is current loop index|
|currentloopname|string|If no value is provided for loopName, this loopname is used |
|isprepare|bool|if this key is exist, then process for prepare, not care the value is true or false|

### [Array and object](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-array)

### [Comparison](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-comparison)

### [Deployment](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-deployment)

TODO£º
* environment
* deployment

### [Logical](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-logical)

### [Numeric](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-numeric)

* [copyIndex](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-numeric#copyindex)

copyIndex need two item in context:

|key|vaule|description|
|-|-|-|
|copyindex|Dictionary<string,int>|the key is loopname,and the value is current loop index|
|currentloopname|string|If no value is provided for loopName, this loopname is used |
 


### [Resource](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-resource)


### [String](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-string)

