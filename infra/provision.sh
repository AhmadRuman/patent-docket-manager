#!/usr/bin/env bash
# Provisions the Azure resources and prints the values the GitHub deploy workflow needs.
# Requires: Azure CLI (az login done) and a subscription with the Azure SQL free offer available.
#
#   SQL_ADMIN_PASSWORD='...' ./infra/provision.sh [resource-group] [location]
set -euo pipefail

RG="${1:-rg-patent-docket-demo}"
LOCATION="${2:-eastus2}"
: "${SQL_ADMIN_PASSWORD:?Set SQL_ADMIN_PASSWORD (12+ chars, mixed case, digit, symbol)}"

az group create --name "$RG" --location "$LOCATION" --tags data=fictional-demo-only >/dev/null

outputs=$(az deployment group create \
  --resource-group "$RG" \
  --template-file "$(dirname "$0")/main.bicep" \
  --parameters sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
  --query properties.outputs -o json)

api_name=$(jq -r .apiName.value <<<"$outputs")
swa_name=$(jq -r .staticWebAppName.value <<<"$outputs")
swa_token=$(az staticwebapp secrets list --name "$swa_name" --resource-group "$RG" --query properties.apiKey -o tsv)

cat <<MSG

Provisioned:
  API:  $(jq -r .apiUrl.value <<<"$outputs")
  Web:  $(jq -r .webUrl.value <<<"$outputs")

Add these to GitHub (Settings > Secrets and variables > Actions):
  Variable AZURE_WEBAPP_NAME             = $api_name
  Secret   AZURE_STATIC_WEB_APPS_API_TOKEN = (printed below, keep it private)
  Secrets  AZURE_CLIENT_ID / AZURE_TENANT_ID / AZURE_SUBSCRIPTION_ID for OIDC login (see README)

$swa_token
MSG
