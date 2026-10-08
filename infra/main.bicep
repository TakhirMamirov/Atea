// Cloud Reports — Azure infrastructure.
//
// Resources (one resource group):
//   - User-assigned managed identity shared by the web app and the function app (no secrets in config)
//   - Azure SQL logical server (Entra-only auth, the identity is the Entra admin) + database
//   - Key Vault (RBAC) holding the OpenWeatherMap API key, referenced from function app settings
//   - Linux App Service (ASP.NET Core API + React SPA)
//   - Function app on the Flex Consumption plan (timer trigger) + its storage account
//   - Log Analytics + Application Insights
//
// The deploying principal needs Owner (or Contributor + Role Based Access Control Administrator)
// on the resource group because role assignments are created here.

targetScope = 'resourceGroup'

@description('Short name used to derive resource names (lowercase letters and digits).')
@minLength(3)
@maxLength(12)
param appName string = 'cloudrep'

@description('Region for all resources. Must support the Flex Consumption plan.')
param location string = resourceGroup().location

@description('OpenWeatherMap API key. Stored in Key Vault; never placed in app settings directly.')
@secure()
param openWeatherMapApiKey string

@description('Cities to ingest every minute.')
param cities array = [
  'London'
  'Rome'
  'Riga'
]

@description('SQL Database SKU. Basic (~5 USD/month) suits a database written every minute; use GP_S_Gen5_1 for serverless.')
param sqlSkuName string = 'Basic'

@description('SQL Database tier matching sqlSkuName (Basic, Standard, GeneralPurpose).')
param sqlSkuTier string = 'Basic'

@description('App Service plan SKU for the web app.')
param webSkuName string = 'B1'

var token = toLower(uniqueString(resourceGroup().id, appName))
var names = {
  identity: 'id-${appName}-${token}'
  logs: 'log-${appName}-${token}'
  insights: 'appi-${appName}-${token}'
  keyVault: 'kv-${take(appName, 8)}-${take(token, 11)}'
  sqlServer: 'sql-${appName}-${token}'
  sqlDatabase: 'sqldb-${appName}'
  storage: take('st${appName}${token}', 24)
  webPlan: 'asp-${appName}-${token}'
  web: 'app-${appName}-${token}'
  functionPlan: 'fcp-${appName}-${token}'
  function: 'func-${appName}-${token}'
}
var deploymentContainerName = 'app-package'

var roles = {
  storageBlobDataOwner: 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
  storageQueueDataContributor: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
  storageTableDataContributor: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
  keyVaultSecretsUser: '4633458b-17de-408a-b874-0445c86b69e6'
  monitoringMetricsPublisher: '3913510d-42f4-4e42-8a64-420c390055eb'
}

// ---------------------------------------------------------------------------
// Identity & monitoring
// ---------------------------------------------------------------------------

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: names.identity
  location: location
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: names.logs
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: names.insights
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource insightsPublisher 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(appInsights.id, identity.id, roles.monitoringMetricsPublisher)
  scope: appInsights
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.monitoringMetricsPublisher)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// ---------------------------------------------------------------------------
// Key Vault
// ---------------------------------------------------------------------------

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: names.keyVault
  location: location
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    publicNetworkAccess: 'Enabled'
  }
}

resource apiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'OpenWeatherMap-ApiKey'
  properties: {
    value: openWeatherMapApiKey
  }
}

resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, identity.id, roles.keyVaultSecretsUser)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.keyVaultSecretsUser)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// ---------------------------------------------------------------------------
// Azure SQL (Entra-only authentication; the managed identity is the server admin)
// ---------------------------------------------------------------------------

resource sqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: names.sqlServer
  location: location
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Application'
      login: identity.name
      // For applications (incl. managed identities) SQL identifies the principal by its client ID.
      sid: identity.properties.clientId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

// Lets App Service and Flex Consumption (dynamic outbound IPs) reach the server.
resource sqlAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01' = {
  parent: sqlServer
  name: names.sqlDatabase
  location: location
  sku: {
    name: sqlSkuName
    tier: sqlSkuTier
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDatabase.name};Authentication=Active Directory Managed Identity;User Id=${identity.properties.clientId};Encrypt=True;Connect Timeout=60'

// ---------------------------------------------------------------------------
// Web app (ASP.NET Core MVC API + React SPA)
// ---------------------------------------------------------------------------

resource webPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: names.webPlan
  location: location
  kind: 'linux'
  sku: { name: webSkuName }
  properties: {
    reserved: true
  }
}

resource web 'Microsoft.Web/sites@2024-04-01' = {
  name: names.web
  location: location
  kind: 'app,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    serverFarmId: webPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      healthCheckPath: '/health'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'Database__MigrateOnStartup', value: 'true' }
      ]
      connectionStrings: [
        { name: 'CloudReports', type: 'SQLAzure', connectionString: sqlConnectionString }
      ]
    }
  }
}

// ---------------------------------------------------------------------------
// Function app (Flex Consumption)
// ---------------------------------------------------------------------------

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: names.storage
  location: location
  kind: 'StorageV2'
  sku: { name: 'Standard_LRS' }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
  }

  resource blobServices 'blobServices' = {
    name: 'default'

    resource deploymentContainer 'containers' = {
      name: deploymentContainerName
      properties: { publicAccess: 'None' }
    }
  }
}

resource storageRoles 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for roleId in [roles.storageBlobDataOwner, roles.storageQueueDataContributor, roles.storageTableDataContributor]: {
    name: guid(storage.id, identity.id, roleId)
    scope: storage
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleId)
      principalId: identity.properties.principalId
      principalType: 'ServicePrincipal'
    }
  }
]

resource functionPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: names.functionPlan
  location: location
  kind: 'functionapp'
  sku: {
    tier: 'FlexConsumption'
    name: 'FC1'
  }
  properties: {
    reserved: true
  }
}

var cityAppSettings = reduce(
  map(range(0, length(cities)), i => { 'WeatherIngestion__Cities__${i}': cities[i] }),
  {},
  (acc, item) => union(acc, item)
)

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: names.function
  location: location
  kind: 'functionapp,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    serverFarmId: functionPlan.id
    httpsOnly: true
    keyVaultReferenceIdentity: identity.id
    siteConfig: {
      minTlsVersion: '1.2'
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storage.properties.primaryEndpoints.blob}${deploymentContainerName}'
          authentication: {
            type: 'UserAssignedIdentity'
            userAssignedIdentityResourceId: identity.id
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 40
        instanceMemoryMB: 2048
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
  }

  resource appSettings 'config' = {
    name: 'appsettings'
    properties: union(
      {
        AzureWebJobsStorage__accountName: storage.name
        AzureWebJobsStorage__credential: 'managedidentity'
        AzureWebJobsStorage__clientId: identity.properties.clientId
        APPLICATIONINSIGHTS_CONNECTION_STRING: appInsights.properties.ConnectionString
        APPLICATIONINSIGHTS_AUTHENTICATION_STRING: 'ClientId=${identity.properties.clientId};Authorization=AAD'
        WeatherIngestionSchedule: '0 * * * * *'
        OpenWeatherMap__ApiKey: '@Microsoft.KeyVault(SecretUri=${apiKeySecret.properties.secretUri})'
        ConnectionStrings__CloudReports: sqlConnectionString
      },
      cityAppSettings
    )
  }

  dependsOn: [
    storageRoles
    keyVaultSecretsUser
  ]
}

// ---------------------------------------------------------------------------
// Outputs consumed by the deployment workflow
// ---------------------------------------------------------------------------

output webAppName string = web.name
output webAppUrl string = 'https://${web.properties.defaultHostName}'
output functionAppName string = functionApp.name
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDatabase.name
