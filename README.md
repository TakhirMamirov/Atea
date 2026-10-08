# Cloud Reports

Collects current weather for **London, Rome and Riga** from OpenWeatherMap every minute, stores the full payloads and
every fetch attempt in Azure SQL, and presents them in a React UI with a temperature chart and a fetch-log page.

## Architecture

```
                  every minute
OpenWeatherMap ◄──────────────── Azure Function (Flex Consumption, timer)
                                        │
                                        ▼
                                 Azure SQL Database ◄──── ASP.NET Core MVC API ◄──── React SPA
                                 (EF Core migrations)     (App Service, also serves the SPA)
```

| Project | Contents |
|---|---|
| `CloudReports.Domain` | Database entities: `WeatherReading`, `FetchLog` |
| `CloudReports.Application` | Business logic: fetching and saving weather, chart data, log queries |
| `CloudReports.Infrastructure` | EF Core database access and migrations, OpenWeatherMap client |
| `CloudReports.Functions` | Azure Function that runs the fetch every minute |
| `CloudReports.Web` | ASP.NET Core API, also serves the React app |
| `client` | React + TypeScript frontend |

### Design notes

- The three cities are fetched in parallel. A failure for one city doesn't stop the others from being saved.
- Long chart ranges are downsampled on the server to at most 500 points per city, keeping the min/max extremes.
- Times are stored in UTC and shown in the viewer's local time zone.
- No passwords in Azure: the apps use a managed identity, and the API key is kept in Key Vault.
- The API is rate-limited per client IP (60 requests per minute), and responses carry standard security headers.
- The database schema is created automatically when the web app starts.

## Run locally

Prerequisites: .NET SDK 10, Node 24, SQL Server LocalDB (installed with Visual Studio),
[Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local), Azurite
(`npm install -g azurite`) and an [OpenWeatherMap API key](https://home.openweathermap.org/api_keys).
Without LocalDB (e.g. on macOS/Linux), `docker-compose.yml` provides SQL Server and Azurite instead.

**Storage emulator** (needed by the Functions host):

```bash
azurite --silent --location .azurite
```

**API + database** (creates the LocalDB database on start, http://localhost:5275):

```bash
dotnet run --project src/CloudReports.Web --launch-profile http
```

**Ingestion function** — copy the example settings and add your API key:

```bash
cp src/CloudReports.Functions/local.settings.example.json src/CloudReports.Functions/local.settings.json
cd src/CloudReports.Functions && func start
```

**Frontend** (http://localhost:5173, proxies `/api` to the API):

```bash
cd client && npm install && npm run dev
```

**Tests**

```bash
dotnet test
cd client && npm test
```

## Deploy to Azure (GitHub Actions)

Every push to `main` builds, tests and deploys everything to the `TEST` resource group.

One-time setup:

1. Run the setup script in Git Bash, after `az login`:

   ```bash
   ./infra/setup-github-oidc.sh <github-user>/<repo>
   ```

2. In GitHub, add the secrets the script prints (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`)
   plus `OPENWEATHERMAP_API_KEY`, and create an environment named `production`.

## API

| Method | Path | Query |
|---|---|---|
| GET | `/api/weather` | `from`, `to` (ISO 8601; defaults to the last 24 h; max 31 days) |
| GET | `/api/fetch-logs` | `page`, `pageSize` (≤ 200), `city`, `isSuccess` |
| GET | `/health` | — (checks database connectivity) |

The OpenAPI document is at `/openapi/v1.json` in Development. Requests over the rate limit get `429 Too Many Requests`.
