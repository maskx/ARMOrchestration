# ARMCreator

## [ARM Functions](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions)

```CSharp
ARMFunctions.Evaluate(string function, Dictionary<string, object> context)
```
The context need 3 value:

|key|value|description|
|-|-|-|
|parametersdefine||the content of parameters property in template file|
|variabledefine||the content of variables property in template file|
|parameters||the content of parameters file|

### [Array and object](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-array)

### [Comparison](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-comparison)

### [Deployment](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-deployment)

TODO£º
* deployment
* environment

### [Logical](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-logical)

### [Numeric](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-numeric)

* [copyIndex](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-numeric#copyindex)

copyIndex need two item in context:

|key|vaule|description|
|-|-|-|
|copyindex|Dictionary<string,int>|the key is loopname,and the value is current loop index|
|copyindexcurrentloopname|string|If no value is provided for loopName, this loopname is used |
 


### [Resource](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-resource)

TODO:
* extensionResourceId
* list*
* providers
* reference
* resourceGroup
* resourceId
* subscription
* subscriptionResourceId
* tenantResourceId

### [String](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-string)

TODO:
* [uniqueString](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-template-functions-string#uniquestring)