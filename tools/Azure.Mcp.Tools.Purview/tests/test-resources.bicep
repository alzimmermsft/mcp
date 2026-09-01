// Purview data security and governance APIs are tenant-scoped Microsoft Graph APIs.
// Live test runs still require deployment metadata, so this template intentionally deploys no ARM resources.
targetScope = 'resourceGroup'

@minLength(3)
@maxLength(24)
@description('The base resource name.')
param baseName string

@description('The application ID used by the live tests.')
param testApplicationId string

@description('The object ID of the principal used by the live tests.')
param testApplicationOid string = deployer().objectId

@description('The object ID of the licensed Microsoft Entra user used to evaluate Purview policies.')
param purviewTestUserId string = ''

@description('The email address of the licensed user to which the Purview labels are published.')
param purviewTestUserEmail string = ''

@description('The ID of a file-scoped sensitivity label with lower priority than the high-priority test label.')
param purviewTestLowPriorityLabelId string = ''

@description('The ID of a high-priority, file-scoped sensitivity label with admin-defined encryption for the test user.')
param purviewTestHighPriorityLabelId string = ''

var location = resourceGroup().location
var tenantId = subscription().tenantId

output location string = location
output purviewTestHighPriorityLabelId string = purviewTestHighPriorityLabelId
output purviewTestLowPriorityLabelId string = purviewTestLowPriorityLabelId
output purviewTestUserEmail string = purviewTestUserEmail
output purviewTestUserId string = purviewTestUserId
output resourceBaseName string = baseName
output tenantId string = tenantId
output testApplicationId string = testApplicationId
output testApplicationOid string = testApplicationOid
