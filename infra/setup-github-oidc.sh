#!/usr/bin/env bash
# One-time setup: creates an Entra app registration that GitHub Actions uses to sign in to Azure
# via OpenID Connect (no client secret), grants it access to the target resource group, and prints
# the values to store as GitHub secrets.
#
# Usage: ./infra/setup-github-oidc.sh <github-owner>/<repo> [resource-group] [location]
#   resource-group  defaults to TEST. An existing group is reused (its region is kept);
#                   a missing one is created in <location>.
#   location        only needed when the group does not exist yet (e.g. northeurope).
#   OIDC_SUBJECT    optional env var: an extra federated subject to trust (see below).
# Requires: Azure CLI (az login) with rights to create app registrations and assign roles.
set -euo pipefail

# Git Bash on Windows rewrites arguments starting with "/" into file paths, which would mangle
# Azure resource IDs such as /subscriptions/...; disable that conversion.
export MSYS_NO_PATHCONV=1

REPO="${1:?GitHub repository, e.g. my-user/cloud-reports}"
RESOURCE_GROUP="${2:-TEST}"
LOCATION="${3:-}"
APP_NAME="github-${REPO//\//-}-deploy"

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
TENANT_ID=$(az account show --query tenantId -o tsv)

# Resource providers must be registered once per subscription. The pipeline can't do this itself
# because it only has access to the resource group.
echo "Registering resource providers (once per subscription)..."
for ns in Microsoft.ManagedIdentity Microsoft.KeyVault Microsoft.Sql Microsoft.Web Microsoft.Storage \
          Microsoft.Insights Microsoft.OperationalInsights; do
  az provider register --namespace "$ns" --wait --output none
done

if [[ "$(az group exists --name "$RESOURCE_GROUP")" == "true" ]]; then
  LOCATION=$(az group show --name "$RESOURCE_GROUP" --query location -o tsv)
  echo "Using existing resource group $RESOURCE_GROUP ($LOCATION)."
else
  : "${LOCATION:?Resource group $RESOURCE_GROUP does not exist; pass a location as the third argument}"
  echo "Creating resource group $RESOURCE_GROUP in $LOCATION..."
  az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none
fi
RG_ID=$(az group show --name "$RESOURCE_GROUP" --query id -o tsv)

echo "Creating app registration $APP_NAME..."
CLIENT_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
az ad sp create --id "$CLIENT_ID" --output none 2>/dev/null || true
SP_OBJECT_ID=$(az ad sp show --id "$CLIENT_ID" --query id -o tsv)

add_federated_credential() {
  local name="$1" subject="$2"
  echo "Adding federated credential: $subject"
  az ad app federated-credential create --id "$CLIENT_ID" --parameters "{
    \"name\": \"$name\",
    \"issuer\": \"https://token.actions.githubusercontent.com\",
    \"subject\": \"$subject\",
    \"audiences\": [\"api://AzureADTokenExchange\"]
  }" --output none 2>/dev/null || echo "  (already exists)"
}

add_federated_credential "github-production" "repo:${REPO}:environment:production"

# Some repositories send a subject that includes numeric IDs, e.g.
#   repo:owner@123/repo@456:environment:production
# If the Azure login step reports "No matching federated identity record found for presented
# assertion subject '...'", re-run this script with OIDC_SUBJECT set to that exact subject.
if [[ -n "${OIDC_SUBJECT:-}" ]]; then
  add_federated_credential "github-production-ids" "$OIDC_SUBJECT"
fi

# Owner on the resource group only: the Bicep template creates role assignments for the managed identity.
echo "Granting Owner on $RESOURCE_GROUP..."
az role assignment create --assignee-object-id "$SP_OBJECT_ID" --assignee-principal-type ServicePrincipal \
  --role Owner --scope "$RG_ID" --output none

cat <<EOF

Done. Resources will be deployed to $RESOURCE_GROUP ($LOCATION).

Configure the GitHub repository (Settings > Secrets and variables > Actions > Secrets):

    AZURE_CLIENT_ID          = $CLIENT_ID
    AZURE_TENANT_ID          = $TENANT_ID
    AZURE_SUBSCRIPTION_ID    = $SUBSCRIPTION_ID
    OPENWEATHERMAP_API_KEY   = <your OpenWeatherMap API key>

The deploy workflow targets the TEST resource group by default. If you used a different group,
also add the repository variable AZURE_RESOURCE_GROUP = $RESOURCE_GROUP.

Also create a GitHub environment named 'production' (Settings > Environments).
EOF
