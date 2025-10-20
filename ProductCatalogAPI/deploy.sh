#!/bin/bash

# Azure Deployment Script for ProductCatalogAPI (Linux/macOS)

set -e

# Default values
LOCATION="East US"
ENVIRONMENT="dev"
SQL_ADMIN_PASSWORD="YourSecurePassword123!"

# Function to display usage
usage() {
    echo "Usage: $0 -s <subscription-id> -g <resource-group-name> [-l <location>] [-e <environment>] [-p <sql-password>]"
    echo "  -s: Azure Subscription ID (required)"
    echo "  -g: Resource Group Name (required)"
    echo "  -l: Azure Location (default: East US)"
    echo "  -e: Environment (default: dev)"
    echo "  -p: SQL Admin Password (default: YourSecurePassword123!)"
    exit 1
}

# Parse command line arguments
while getopts "s:g:l:e:p:h" opt; do
    case $opt in
        s) SUBSCRIPTION_ID="$OPTARG" ;;
        g) RESOURCE_GROUP_NAME="$OPTARG" ;;
        l) LOCATION="$OPTARG" ;;
        e) ENVIRONMENT="$OPTARG" ;;
        p) SQL_ADMIN_PASSWORD="$OPTARG" ;;
        h) usage ;;
        *) usage ;;
    esac
done

# Check required parameters
if [ -z "$SUBSCRIPTION_ID" ] || [ -z "$RESOURCE_GROUP_NAME" ]; then
    echo "Error: Subscription ID and Resource Group Name are required."
    usage
fi

echo "🚀 Starting deployment to Azure..."
echo "Subscription: $SUBSCRIPTION_ID"
echo "Resource Group: $RESOURCE_GROUP_NAME"
echo "Location: $LOCATION"
echo "Environment: $ENVIRONMENT"

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "❌ Azure CLI is not installed. Please install it first."
    echo "Visit: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if .NET CLI is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET CLI is not installed. Please install .NET 8 SDK first."
    echo "Visit: https://dotnet.microsoft.com/download"
    exit 1
fi

# Login to Azure (if not already logged in)
echo "🔐 Checking Azure login status..."
if ! az account show &> /dev/null; then
    echo "Logging in to Azure..."
    az login
fi

# Set subscription
echo "📋 Setting subscription to $SUBSCRIPTION_ID..."
az account set --subscription "$SUBSCRIPTION_ID"

# Create resource group if it doesn't exist
echo "📁 Creating resource group if it doesn't exist..."
az group create --name "$RESOURCE_GROUP_NAME" --location "$LOCATION" --output none

# Deploy Bicep template
echo "🏗️  Deploying infrastructure using Bicep template..."
DEPLOYMENT_NAME="ProductCatalogAPI-$(date +%Y%m%d-%H%M%S)"

az deployment group create \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --template-file bicep/main.bicep \
    --name "$DEPLOYMENT_NAME" \
    --parameters location="$LOCATION" \
                environment="$ENVIRONMENT" \
                sqlAdminPassword="$SQL_ADMIN_PASSWORD" \
    --output table

if [ $? -eq 0 ]; then
    echo "✅ Infrastructure deployment completed successfully!"
else
    echo "❌ Infrastructure deployment failed!"
    exit 1
fi

# Get deployment outputs
WEB_APP_URL=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$DEPLOYMENT_NAME" \
    --query "properties.outputs.webAppUrl.value" \
    --output tsv)

SQL_SERVER_FQDN=$(az deployment group show \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$DEPLOYMENT_NAME" \
    --query "properties.outputs.sqlServerFqdn.value" \
    --output tsv)

echo "🌐 Web App URL: $WEB_APP_URL"
echo "🗄️  SQL Server FQDN: $SQL_SERVER_FQDN"

# Build and publish the application
echo "🔨 Building and publishing the application..."
dotnet clean
dotnet restore
dotnet build --configuration Release
dotnet publish --configuration Release --output ./publish

if [ $? -eq 0 ]; then
    echo "✅ Application built successfully!"
else
    echo "❌ Application build failed!"
    exit 1
fi

# Create deployment package
echo "📦 Creating deployment package..."
ZIP_PATH="./ProductCatalogAPI.zip"
rm -f "$ZIP_PATH"

# Create zip file
cd publish
zip -r "../$ZIP_PATH" .
cd ..

# Deploy to App Service
WEB_APP_NAME="productcatalog-api-$ENVIRONMENT"
echo "🚀 Deploying application to App Service: $WEB_APP_NAME..."

az webapp deployment source config-zip \
    --resource-group "$RESOURCE_GROUP_NAME" \
    --name "$WEB_APP_NAME" \
    --src "$ZIP_PATH"

if [ $? -eq 0 ]; then
    echo "✅ Application deployment completed successfully!"
    
    # Clean up
    rm -f "$ZIP_PATH"
    rm -rf "./publish"
    
else
    echo "❌ Application deployment failed!"
    exit 1
fi

echo ""
echo "🎉 Deployment completed successfully!"
echo "🌐 Your application is available at: https://$WEB_APP_NAME.azurewebsites.net"
echo ""
echo "📋 Next steps:"
echo "1. Configure your database connection string in the Azure portal"
echo "2. Run database migrations"
echo "3. Configure custom domains and SSL certificates if needed"
echo "4. Set up monitoring and alerts"
echo ""
echo "💡 To run database migrations, use:"
echo "   dotnet ef database update --connection \"Server=tcp:$SQL_SERVER_FQDN,1433;Initial Catalog=productcatalog-api-db-$ENVIRONMENT;Persist Security Info=False;User ID=sqladmin;Password=$SQL_ADMIN_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;\""