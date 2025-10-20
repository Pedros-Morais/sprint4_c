# Deployment Guide - ProductCatalogAPI

Este guia fornece instruções detalhadas para fazer o deploy da ProductCatalogAPI no Microsoft Azure.

## 📋 Pré-requisitos

### Ferramentas Necessárias
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (versão 2.0 ou superior)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PowerShell](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell) (para Windows) ou Bash (para Linux/macOS)
- Conta do Microsoft Azure com permissões de Contributor

### Recursos Azure Necessários
- Azure Subscription ativa
- Resource Group (será criado automaticamente se não existir)

## 🚀 Métodos de Deployment

### Método 1: Script Automatizado (Recomendado)

#### Para Windows (PowerShell)
```powershell
.\deploy.ps1 -SubscriptionId "your-subscription-id" -ResourceGroupName "rg-productcatalog"
```

#### Para Linux/macOS (Bash)
```bash
./deploy.sh -s "your-subscription-id" -g "rg-productcatalog"
```

### Método 2: Deployment Manual

#### Passo 1: Login no Azure
```bash
az login
az account set --subscription "your-subscription-id"
```

#### Passo 2: Criar Resource Group
```bash
az group create --name "rg-productcatalog" --location "East US"
```

#### Passo 3: Deploy da Infraestrutura
```bash
az deployment group create \
    --resource-group "rg-productcatalog" \
    --template-file bicep/main.bicep \
    --parameters @bicep/parameters.json
```

#### Passo 4: Build da Aplicação
```bash
dotnet clean
dotnet restore
dotnet build --configuration Release
dotnet publish --configuration Release --output ./publish
```

#### Passo 5: Deploy da Aplicação
```bash
# Criar pacote ZIP
zip -r ProductCatalogAPI.zip ./publish/*

# Deploy no App Service
az webapp deployment source config-zip \
    --resource-group "rg-productcatalog" \
    --name "productcatalog-api-dev" \
    --src ProductCatalogAPI.zip
```

## 🏗️ Infraestrutura Provisionada

O template Bicep cria os seguintes recursos:

### Recursos Principais
- **App Service Plan** (B1 SKU por padrão)
- **Web App** (Linux com .NET 8)
- **SQL Server** com autenticação SQL
- **SQL Database** (S0 SKU por padrão)
- **Application Insights** para monitoramento
- **Log Analytics Workspace** para logs

### Configurações de Segurança
- HTTPS obrigatório
- TLS 1.2 mínimo
- Firewall do SQL Server configurado para Azure Services
- Usuário não-root no container Docker

## 🔧 Configuração Pós-Deployment

### 1. Configurar Connection String
A connection string é configurada automaticamente, mas você pode verificar/atualizar:

```bash
az webapp config connection-string set \
    --resource-group "rg-productcatalog" \
    --name "productcatalog-api-dev" \
    --connection-string-type SQLAzure \
    --settings DefaultConnection="Server=tcp:your-server.database.windows.net,1433;Initial Catalog=your-database;Persist Security Info=False;User ID=sqladmin;Password=YourPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
```

### 2. Executar Migrations do Entity Framework
```bash
# Instalar EF Tools se necessário
dotnet tool install --global dotnet-ef

# Executar migrations
dotnet ef database update --connection "your-connection-string"
```

### 3. Configurar Variáveis de Ambiente
```bash
az webapp config appsettings set \
    --resource-group "rg-productcatalog" \
    --name "productcatalog-api-dev" \
    --settings \
        ASPNETCORE_ENVIRONMENT="Production" \
        CepApiUrl="https://viacep.com.br/ws" \
        CurrencyApiUrl="https://api.exchangerate-api.com/v4/latest"
```

## 🐳 Deployment com Docker

### Build da Imagem Docker
```bash
docker build -t productcatalog-api .
```

### Deploy no Azure Container Registry
```bash
# Criar ACR
az acr create --resource-group "rg-productcatalog" --name "productcatalogacr" --sku Basic

# Login no ACR
az acr login --name "productcatalogacr"

# Tag e push da imagem
docker tag productcatalog-api productcatalogacr.azurecr.io/productcatalog-api:latest
docker push productcatalogacr.azurecr.io/productcatalog-api:latest
```

### Deploy no Azure Container Instances
```bash
az container create \
    --resource-group "rg-productcatalog" \
    --name "productcatalog-api-container" \
    --image productcatalogacr.azurecr.io/productcatalog-api:latest \
    --cpu 1 \
    --memory 1.5 \
    --registry-login-server productcatalogacr.azurecr.io \
    --registry-username "productcatalogacr" \
    --registry-password "your-acr-password" \
    --dns-name-label "productcatalog-api" \
    --ports 8080
```

## 🔄 CI/CD com Azure DevOps

### Configurar Service Connection
1. No Azure DevOps, vá para Project Settings > Service connections
2. Crie uma nova service connection do tipo "Azure Resource Manager"
3. Configure com o nome "ProductCatalog-ServiceConnection"

### Configurar Pipeline
1. Importe o arquivo `azure-pipelines.yml` no seu repositório Azure DevOps
2. Configure as variáveis necessárias:
   - `azureSubscription`: Nome da service connection
   - `appName`: Nome da aplicação
   - `resourceGroupName`: Nome do resource group

## 📊 Monitoramento e Logs

### Application Insights
- Métricas de performance automáticas
- Rastreamento de dependências
- Logs de aplicação centralizados

### Verificar Logs
```bash
# Logs do App Service
az webapp log tail --resource-group "rg-productcatalog" --name "productcatalog-api-dev"

# Logs do Application Insights
az monitor app-insights query \
    --app "productcatalog-api-ai-dev" \
    --analytics-query "requests | limit 10"
```

## 🔒 Segurança e Boas Práticas

### Configurações de Segurança Implementadas
- Conexões HTTPS obrigatórias
- Senhas armazenadas como SecureString
- Usuário não-root no container
- Firewall do SQL Server restritivo
- Health checks configurados

### Recomendações Adicionais
1. **Key Vault**: Armazene secrets no Azure Key Vault
2. **Managed Identity**: Use identidades gerenciadas para autenticação
3. **Custom Domains**: Configure domínios personalizados com SSL
4. **Backup**: Configure backup automático do banco de dados
5. **Scaling**: Configure auto-scaling baseado em métricas

## 🌍 Ambientes Múltiplos

### Desenvolvimento
```bash
./deploy.sh -s "subscription-id" -g "rg-productcatalog-dev" -e "dev"
```

### Staging
```bash
./deploy.sh -s "subscription-id" -g "rg-productcatalog-staging" -e "staging"
```

### Produção
```bash
./deploy.sh -s "subscription-id" -g "rg-productcatalog-prod" -e "prod"
```

## 🆘 Troubleshooting

### Problemas Comuns

#### 1. Erro de Autenticação Azure
```bash
az logout
az login
```

#### 2. Erro de Permissões
Verifique se sua conta tem permissões de Contributor no subscription.

#### 3. Erro de Build .NET
```bash
dotnet clean
dotnet restore --force
```

#### 4. Erro de Connection String
Verifique se a senha do SQL Server atende aos requisitos de complexidade.

### Logs de Diagnóstico
```bash
# Habilitar logs detalhados
az webapp log config \
    --resource-group "rg-productcatalog" \
    --name "productcatalog-api-dev" \
    --application-logging true \
    --detailed-error-messages true \
    --failed-request-tracing true \
    --web-server-logging filesystem
```

## 📞 Suporte

Para problemas relacionados ao deployment:
1. Verifique os logs do Application Insights
2. Consulte a documentação oficial do Azure
3. Abra uma issue no repositório do projeto

## 🔗 Links Úteis

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure SQL Database Documentation](https://docs.microsoft.com/en-us/azure/azure-sql/)
- [Application Insights Documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Azure DevOps Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/)