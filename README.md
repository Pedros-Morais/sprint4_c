# Product Catalog API

## 👥 Equipe de Desenvolvimento

| Nome | RM |
|------|-----|
| Gustavo Vegi Pedro Henrique Silva de Morais | RM550188 |
| Lucas Rodrigues Delfino | RM98804 |
| Luisa Cristina dos Santos Neves | RM550196 |
| Gabriel Aparecido Cassalho Xavier | RM551889 |
| [Nome do 5º Integrante] | RM99794 |

## 📋 Descrição do Projeto

API REST completa para gerenciamento de catálogo de produtos desenvolvida em ASP.NET Core com Entity Framework. O sistema oferece operações CRUD completas, pesquisas avançadas com LINQ, integração com APIs externas e documentação Swagger.

## 🏗️ Arquitetura

### Tecnologias Utilizadas

- **ASP.NET Core 8.0** - Framework web
- **Entity Framework Core** - ORM para acesso a dados
- **SQL Server** - Banco de dados
- **Swagger/OpenAPI** - Documentação da API
- **LINQ** - Consultas avançadas
- **Azure** - Hospedagem em nuvem

### Estrutura do Projeto

```
ProductCatalogAPI/
├── Controllers/           # Controladores da API
│   ├── ProductsController.cs
│   ├── CategoriesController.cs
│   ├── CustomersController.cs
│   ├── OrdersController.cs
│   ├── ExternalApiController.cs
│   ├── AnalyticsController.cs
│   └── SearchController.cs
├── Models/               # Modelos de dados
│   ├── Product.cs
│   ├── Category.cs
│   ├── Customer.cs
│   ├── Order.cs
│   └── OrderItem.cs
├── Data/                 # Contexto do banco de dados
│   └── ApplicationDbContext.cs
├── Services/             # Serviços externos
│   └── ExternalApiService.cs
├── Migrations/           # Migrações do banco
└── Properties/           # Configurações
```

## 🚀 Funcionalidades

### 1. CRUD Completo (35%)

#### Produtos
- ✅ Criar, ler, atualizar e excluir produtos
- ✅ Gerenciamento de estoque
- ✅ Controle de ativação/desativação
- ✅ Upload de imagens
- ✅ Sistema de avaliações

#### Categorias
- ✅ Gerenciamento completo de categorias
- ✅ Estatísticas por categoria
- ✅ Relacionamento com produtos

#### Clientes
- ✅ Cadastro e gerenciamento de clientes
- ✅ Histórico de compras
- ✅ Análise de comportamento

#### Pedidos
- ✅ Criação e gerenciamento de pedidos
- ✅ Controle de status
- ✅ Relatórios de vendas
- ✅ Gestão de estoque automática

### 2. Pesquisas com LINQ (10%)

#### Pesquisas Avançadas
- ✅ **AnalyticsController**: Dashboard com estatísticas gerais
- ✅ **SearchController**: Pesquisas complexas com múltiplos filtros
- ✅ Produtos similares baseados em características
- ✅ Análise comportamental de clientes
- ✅ Padrões de compra por período
- ✅ Top produtos mais vendidos
- ✅ Análise de vendas por categoria
- ✅ Clientes mais valiosos
- ✅ Tendências de vendas
- ✅ Performance por marca

#### Exemplos de LINQ Complexo
```csharp
// Análise de produtos similares
var similarProducts = await _context.Products
    .Where(p => p.IsActive && p.Id != id)
    .Where(p => 
        p.CategoryId == product.CategoryId ||
        (p.Brand != null && product.Brand != null && p.Brand == product.Brand) ||
        (p.Price >= product.Price * 0.8m && p.Price <= product.Price * 1.2m))
    .OrderBy(p => p.CategoryId == product.CategoryId ? 0 : 1)
    .ThenBy(p => p.Brand == product.Brand ? 0 : 1)
    .ThenBy(p => Math.Abs(p.Price - product.Price))
    .ToListAsync();

// Análise de padrões de compra
var patterns = await _context.Orders
    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month, DayOfWeek = (int)o.OrderDate.DayOfWeek })
    .Select(g => new {
        Period = g.Key,
        OrderCount = g.Count(),
        TotalRevenue = g.Sum(o => o.TotalAmount),
        TopCategories = g.SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.Product.Category.Name)
            .OrderByDescending(cg => cg.Sum(oi => oi.Quantity))
            .Take(3)
    })
    .ToListAsync();
```

### 3. Endpoints com APIs Externas (20%)

#### Integrações Implementadas
- ✅ **CEP API**: Consulta de endereços por CEP
- ✅ **Currency API**: Cotações e conversão de moedas
- ✅ **Random User API**: Geração de usuários aleatórios
- ✅ **IP Geolocation**: Localização por IP
- ✅ **Bitcoin API**: Cotação do Bitcoin
- ✅ **Countries API**: Informações sobre países

#### Exemplos de Uso
```http
GET /api/ExternalApi/cep/01310-100
GET /api/ExternalApi/currency/rates
GET /api/ExternalApi/currency/convert?from=USD&to=BRL&amount=100
GET /api/ExternalApi/random-users?count=5
```

### 4. Documentação Swagger (10%)

- ✅ Documentação completa de todos os endpoints
- ✅ Exemplos de requisições e respostas
- ✅ Descrições detalhadas dos parâmetros
- ✅ Modelos de dados documentados
- ✅ Interface interativa para testes

### 5. Arquitetura em Diagramas (10%)

- ✅ Diagrama de arquitetura da aplicação
- ✅ Modelo de dados (ERD)
- ✅ Fluxo de dados
- ✅ Integração com APIs externas

### 6. Publicação em Nuvem (15%)

- ✅ Configuração para Azure App Service
- ✅ Banco de dados Azure SQL
- ✅ Variáveis de ambiente
- ✅ CI/CD pipeline

## 🛠️ Instalação e Configuração

### Pré-requisitos

- .NET 8.0 SDK
- SQL Server (LocalDB ou instância completa)
- Visual Studio 2022 ou VS Code

### Passos para Instalação

1. **Clone o repositório**
```bash
git clone [URL_DO_REPOSITORIO]
cd ProductCatalogAPI
```

2. **Restaure os pacotes**
```bash
dotnet restore
```

3. **Configure a string de conexão**
Edite o arquivo `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProductCatalogDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

4. **Execute as migrações**
```bash
dotnet ef database update
```

5. **Execute a aplicação**
```bash
dotnet run
```

6. **Acesse a documentação Swagger**
```
https://localhost:7000/swagger
```

## 📊 Endpoints Principais

### Produtos
- `GET /api/Products` - Lista todos os produtos
- `GET /api/Products/{id}` - Busca produto por ID
- `POST /api/Products` - Cria novo produto
- `PUT /api/Products/{id}` - Atualiza produto
- `DELETE /api/Products/{id}` - Remove produto
- `GET /api/Products/search` - Pesquisa avançada
- `GET /api/Products/category/{categoryId}` - Produtos por categoria
- `GET /api/Products/low-stock` - Produtos com estoque baixo

### Analytics
- `GET /api/Analytics/dashboard` - Dashboard geral
- `GET /api/Analytics/top-selling-products` - Produtos mais vendidos
- `GET /api/Analytics/sales-by-category` - Vendas por categoria
- `GET /api/Analytics/top-customers` - Melhores clientes
- `GET /api/Analytics/monthly-sales` - Vendas mensais
- `GET /api/Analytics/sales-trends` - Tendências de vendas

### Pesquisas Avançadas
- `GET /api/Search/products/advanced` - Pesquisa avançada de produtos
- `GET /api/Search/products/{id}/similar` - Produtos similares
- `GET /api/Search/customers/behavior` - Análise comportamental
- `GET /api/Search/purchase-patterns` - Padrões de compra

### APIs Externas
- `GET /api/ExternalApi/cep/{cep}` - Consulta CEP
- `GET /api/ExternalApi/currency/rates` - Taxas de câmbio
- `GET /api/ExternalApi/random-users` - Usuários aleatórios

## 🔧 Configuração de Ambiente

### Desenvolvimento
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProductCatalogDB;Trusted_Connection=true"
  },
  "ExternalApis": {
    "CepApiUrl": "https://viacep.com.br/ws",
    "CurrencyApiUrl": "https://api.exchangerate-api.com/v4/latest"
  }
}
```

### Produção (Azure)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "[AZURE_SQL_CONNECTION_STRING]"
  }
}
```

## 📈 Métricas e Performance

### Consultas Otimizadas
- Uso de `Include()` para carregamento eager
- Paginação em todas as listagens
- Índices no banco de dados
- Projeções para reduzir dados transferidos

### Exemplo de Consulta Otimizada
```csharp
var products = await _context.Products
    .Include(p => p.Category)
    .Where(p => p.IsActive)
    .Select(p => new {
        p.Id,
        p.Name,
        p.Price,
        CategoryName = p.Category.Name
    })
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

## 🧪 Testes

### Testando com Swagger
1. Acesse `https://localhost:7000/swagger`
2. Explore os endpoints disponíveis
3. Execute testes diretamente na interface

### Exemplos de Teste
```bash
# Criar um produto
curl -X POST "https://localhost:7000/api/Products" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Smartphone XYZ",
    "description": "Smartphone com 128GB",
    "price": 899.99,
    "stock": 50,
    "categoryId": 1,
    "brand": "TechBrand"
  }'

# Pesquisa avançada
curl "https://localhost:7000/api/Search/products/advanced?minPrice=100&maxPrice=1000&brand=TechBrand"
```

## 🚀 Deploy na Azure

### Configuração do Azure
1. Crie um App Service no Azure
2. Configure Azure SQL Database
3. Configure as variáveis de ambiente
4. Faça o deploy via GitHub Actions ou Azure DevOps

### Variáveis de Ambiente
```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=[AZURE_SQL_CONNECTION]
ExternalApis__CepApiUrl=https://viacep.com.br/ws
ExternalApis__CurrencyApiUrl=https://api.exchangerate-api.com/v4/latest
```

## 📝 Estrutura de Dados

### Modelo de Dados Principal
```
Category (1) -----> (*) Product (*) -----> (*) OrderItem
                                              |
Customer (1) -----> (*) Order (1) ----------+
```

### Campos Principais
- **Product**: Id, Name, Description, Price, Stock, CategoryId, Brand, Rating
- **Category**: Id, Name, Description
- **Customer**: Id, Name, Email, Phone, Address, City
- **Order**: Id, CustomerId, OrderDate, TotalAmount, Status
- **OrderItem**: Id, OrderId, ProductId, Quantity, UnitPrice

## 🔒 Segurança

### Implementações de Segurança
- Validação de dados com Data Annotations
- Sanitização de entradas
- Soft delete para preservar integridade
- Validação de estoque antes de vendas
- Controle de transações

## 📞 Suporte

### Contato
- **Desenvolvedor**: [Seu Nome]
- **Email**: [seu.email@exemplo.com]
- **GitHub**: [seu-usuario-github]

### Contribuição
1. Fork o projeto
2. Crie uma branch para sua feature
3. Commit suas mudanças
4. Push para a branch
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo `LICENSE` para mais detalhes.

---

**Desenvolvido com ❤️ por Pedro Morais 
