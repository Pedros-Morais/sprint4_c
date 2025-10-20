# Azure Deployment Script for ProductCatalogAPI
param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$false)]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory=$false)]
    [string]$SqlAdminPassword = "YourSecurePassword123!"
)

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host "Starting deployment to Azure..." -ForegroundColor Green

# Login to Azure (if not already logged in)
try {
    $context = Get-AzContext
    if (!$context) {
        Write-Host "Logging in to Azure..." -ForegroundColor Yellow
        Connect-AzAccount
    }
} catch {
    Write-Host "Logging in to Azure..." -ForegroundColor Yellow
    Connect-AzAccount
}

# Set subscription
Write-Host "Setting subscription to $SubscriptionId..." -ForegroundColor Yellow
Set-AzContext -SubscriptionId $SubscriptionId

# Create resource group if it doesn't exist
$rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if (!$rg) {
    Write-Host "Creating resource group $ResourceGroupName..." -ForegroundColor Yellow
    New-AzResourceGroup -Name $ResourceGroupName -Location $Location
} else {
    Write-Host "Resource group $ResourceGroupName already exists." -ForegroundColor Green
}

# Deploy Bicep template
Write-Host "Deploying infrastructure using Bicep template..." -ForegroundColor Yellow
$deploymentName = "ProductCatalogAPI-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

try {
    $deployment = New-AzResourceGroupDeployment `
        -ResourceGroupName $ResourceGroupName `
        -TemplateFile "bicep/main.bicep" `
        -Name $deploymentName `
        -location $Location `
        -environment $Environment `
        -sqlAdminPassword (ConvertTo-SecureString $SqlAdminPassword -AsPlainText -Force) `
        -Verbose

    Write-Host "Infrastructure deployment completed successfully!" -ForegroundColor Green
    Write-Host "Web App URL: $($deployment.Outputs.webAppUrl.Value)" -ForegroundColor Cyan
    Write-Host "SQL Server FQDN: $($deployment.Outputs.sqlServerFqdn.Value)" -ForegroundColor Cyan
    
} catch {
    Write-Host "Infrastructure deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Build and publish the application
Write-Host "Building and publishing the application..." -ForegroundColor Yellow
try {
    dotnet clean
    dotnet restore
    dotnet build --configuration Release
    dotnet publish --configuration Release --output ./publish
    
    Write-Host "Application built successfully!" -ForegroundColor Green
} catch {
    Write-Host "Application build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Create deployment package
Write-Host "Creating deployment package..." -ForegroundColor Yellow
$zipPath = "./ProductCatalogAPI.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

# Compress the publish folder
Compress-Archive -Path "./publish/*" -DestinationPath $zipPath -Force

# Deploy to App Service
$webAppName = "productcatalog-api-$Environment"
Write-Host "Deploying application to App Service: $webAppName..." -ForegroundColor Yellow

try {
    Publish-AzWebApp `
        -ResourceGroupName $ResourceGroupName `
        -Name $webAppName `
        -ArchivePath $zipPath `
        -Force

    Write-Host "Application deployment completed successfully!" -ForegroundColor Green
    
    # Clean up
    Remove-Item $zipPath -Force
    Remove-Item "./publish" -Recurse -Force
    
} catch {
    Write-Host "Application deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "Your application is available at: https://$webAppName.azurewebsites.net" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Configure your database connection string in the Azure portal" -ForegroundColor White
Write-Host "2. Run database migrations" -ForegroundColor White
Write-Host "3. Configure custom domains and SSL certificates if needed" -ForegroundColor White
Write-Host "4. Set up monitoring and alerts" -ForegroundColor White