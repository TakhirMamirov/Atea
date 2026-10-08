using 'main.bicep'

param appName = 'cloudrep'

// Supplied by the deployment workflow from the OPENWEATHERMAP_API_KEY GitHub secret.
param openWeatherMapApiKey = readEnvironmentVariable('OPENWEATHERMAP_API_KEY')
