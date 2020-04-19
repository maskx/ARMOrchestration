# ARMOrchestration

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

