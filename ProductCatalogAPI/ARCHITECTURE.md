# Arquitetura do Sistema - Product Catalog API

## 🏗️ Visão Geral da Arquitetura

O Product Catalog API foi desenvolvido seguindo os princípios de **Clean Architecture** e **Domain-Driven Design (DDD)**, utilizando o padrão **MVC (Model-View-Controller)** do ASP.NET Core.

## 📊 Diagrama de Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
├─────────────────────────────────────────────────────────────┤
│  Controllers/                                               │
│  ├── ProductsController      ├── AnalyticsController        │
│  ├── CategoriesController    ├── SearchController           │
│  ├── CustomersController     └── ExternalApiController      │
│  └── OrdersController                                       │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION LAYER                         │
├─────────────────────────────────────────────────────────────┤
│  Services/                                                  │
│  └── ExternalApiService                                     │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                     DOMAIN LAYER                            │
├─────────────────────────────────────────────────────────────┤
│  Models/                                                    │
│  ├── Product        ├── Customer                           │
│  ├── Category       ├── Order                              │
│  └── OrderItem                                             │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                 INFRASTRUCTURE LAYER                        │
├─────────────────────────────────────────────────────────────┤
│  Data/                                                      │
│  └── ApplicationDbContext                                   │
│                                                             │
│  External APIs                                              │
│  ├── ViaCEP API      ├── Random User API                   │
│  ├── Currency API    ├── IP Geolocation API                │
│  └── Bitcoin API     └── Countries API                     │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                     DATABASE LAYER                          │
├─────────────────────────────────────────────────────────────┤
│                    SQL Server                               │
│                 ProductCatalogDB                            │
└─────────────────────────────────────────────────────────────┘
```

## 🔄 Fluxo de Dados

### 1. Fluxo de Requisição HTTP

```
Client Request → Controller → Service (if needed) → DbContext → Database
                     ↓
Client Response ← Controller ← Service (if needed) ← DbContext ← Database
```

### 2. Fluxo de Integração com APIs Externas

```
Controller → ExternalApiService → HttpClient → External API
     ↓
Controller ← ExternalApiService ← HttpClient ← External API Response
```

## 🗄️ Modelo de Dados (ERD)

```
┌─────────────────┐         ┌─────────────────┐
│    Category     │         │     Product     │
├─────────────────┤    1:N  ├─────────────────┤
│ Id (PK)         │◄────────┤ Id (PK)         │
│ Name            │         │ Name            │
│ Description     │         │ Description     │
│ CreatedAt       │         │ Price           │
│ IsActive        │         │ Stock           │
└─────────────────┘         │ CategoryId (FK) │
                            │ Brand           │
                            │ Rating          │
                            │ ImageUrl        │
                            │ CreatedAt       │
                            │ UpdatedAt       │
                            │ IsActive        │
                            └─────────────────┘
                                     │
                                     │ N:1
                                     ▼
┌─────────────────┐         ┌─────────────────┐
│    Customer     │         │   OrderItem     │
├─────────────────┤         ├─────────────────┤
│ Id (PK)         │         │ Id (PK)         │
│ Name            │         │ OrderId (FK)    │
│ Email           │         │ ProductId (FK)  │◄─────┐
│ Phone           │         │ Quantity        │      │
│ Address         │         │ UnitPrice       │      │
│ City            │         │ TotalPrice      │      │
│ PostalCode      │         └─────────────────┘      │
│ CreatedAt       │                  │               │
│ IsActive        │                  │ N:1           │
└─────────────────┘                  ▼               │
         │                  ┌─────────────────┐      │
         │ 1:N              │     Order       │      │
         └─────────────────►├─────────────────┤      │
                            │ Id (PK)         │      │
                            │ CustomerId (FK) │      │
                            │ OrderDate       │      │
                            │ TotalAmount     │      │
                            │ Status          │      │
                            │ Notes           │      │
                            └─────────────────┘      │
                                     │               │
                                     │ 1:N           │
                                     └───────────────┘
```

## 🏛️ Padrões Arquiteturais Utilizados

### 1. Repository Pattern (Implícito via Entity Framework)
```csharp
// DbContext atua como Unit of Work e Repository
public class ApplicationDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    // ...
}
```

### 2. Dependency Injection
```csharp
// Program.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<ExternalApiService>();
```

### 3. Service Layer Pattern
```csharp
public class ExternalApiService
{
    private readonly HttpClient _httpClient;
    
    public ExternalApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<CepResponse> GetAddressByCepAsync(string cep)
    {
        // Implementação do serviço
    }
}
```

### 4. Data Transfer Object (DTO) Pattern
```csharp
// Exemplo de projeção para otimizar transferência de dados
var products = await _context.Products
    .Select(p => new
    {
        p.Id,
        p.Name,
        p.Price,
        CategoryName = p.Category.Name
    })
    .ToListAsync();
```

## 🔧 Componentes da Arquitetura

### 1. Controllers (Presentation Layer)

#### ProductsController
- **Responsabilidade**: Gerenciar operações CRUD de produtos
- **Endpoints**: 12 endpoints incluindo pesquisas e relatórios
- **Funcionalidades**: CRUD, pesquisa, controle de estoque

#### AnalyticsController
- **Responsabilidade**: Fornecer análises e relatórios
- **Endpoints**: 10 endpoints de analytics
- **Funcionalidades**: Dashboard, top produtos, vendas por categoria

#### SearchController
- **Responsabilidade**: Pesquisas avançadas com LINQ
- **Endpoints**: 4 endpoints de pesquisa complexa
- **Funcionalidades**: Pesquisa avançada, produtos similares, análise comportamental

### 2. Models (Domain Layer)

#### Entidades Principais
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; }
    // ... outras propriedades
}
```

#### Relacionamentos
- **Category → Products**: 1:N
- **Customer → Orders**: 1:N
- **Order → OrderItems**: 1:N
- **Product → OrderItems**: 1:N

### 3. Data Layer (Infrastructure)

#### ApplicationDbContext
```csharp
public class ApplicationDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configuração de relacionamentos
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);
            
        // Seed data
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Eletrônicos" }
        );
    }
}
```

## 🔄 Integração com APIs Externas

### Arquitetura de Integração

```
┌─────────────────┐    HTTP    ┌─────────────────┐
│ ExternalApi     │◄──────────►│ ExternalApi     │
│ Controller      │            │ Service         │
└─────────────────┘            └─────────────────┘
                                        │
                                        ▼
                               ┌─────────────────┐
                               │   HttpClient    │
                               └─────────────────┘
                                        │
                    ┌───────────────────┼───────────────────┐
                    ▼                   ▼                   ▼
            ┌──────────────┐   ┌──────────────┐   ┌──────────────┐
            │   ViaCEP     │   │ Currency API │   │ Random User  │
            │     API      │   │              │   │     API      │
            └──────────────┘   └──────────────┘   └──────────────┘
```

### APIs Integradas

1. **ViaCEP API**: Consulta de endereços por CEP
2. **ExchangeRate API**: Cotações de moedas
3. **Random User API**: Geração de usuários fictícios
4. **IP Geolocation API**: Localização por IP
5. **CoinGecko API**: Cotação de criptomoedas
6. **REST Countries API**: Informações sobre países

## 📊 Estratégias de Consulta LINQ

### 1. Consultas Simples
```csharp
var products = await _context.Products
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name)
    .ToListAsync();
```

### 2. Consultas com Join
```csharp
var productsWithCategory = await _context.Products
    .Include(p => p.Category)
    .Where(p => p.IsActive)
    .ToListAsync();
```

### 3. Consultas Agregadas
```csharp
var salesByCategory = await _context.OrderItems
    .Include(oi => oi.Product)
    .ThenInclude(p => p.Category)
    .GroupBy(oi => oi.Product.Category.Name)
    .Select(g => new
    {
        CategoryName = g.Key,
        TotalSales = g.Sum(x => x.TotalPrice),
        ProductCount = g.Select(x => x.ProductId).Distinct().Count()
    })
    .ToListAsync();
```

### 4. Consultas Complexas com Múltiplos Joins
```csharp
var customerAnalysis = await _context.Customers
    .Include(c => c.Orders)
    .ThenInclude(o => o.OrderItems)
    .ThenInclude(oi => oi.Product)
    .Select(c => new
    {
        Customer = c,
        TotalSpent = c.Orders.Sum(o => o.TotalAmount),
        FavoriteCategories = c.Orders
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.Product.Category.Name)
            .OrderByDescending(g => g.Sum(oi => oi.Quantity))
            .Take(3)
    })
    .ToListAsync();
```

## 🚀 Configuração para Produção

### Azure App Service Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:server.database.windows.net,1433;Initial Catalog=ProductCatalogDB;Persist Security Info=False;User ID=admin;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Dockerfile (para containerização)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProductCatalogAPI.csproj", "."]
RUN dotnet restore "./ProductCatalogAPI.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ProductCatalogAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductCatalogAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductCatalogAPI.dll"]
```

## 🔒 Considerações de Segurança

### 1. Validação de Dados
```csharp
[Required(ErrorMessage = "Nome é obrigatório")]
[StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres")]
public string Name { get; set; }

[Range(0.01, double.MaxValue, ErrorMessage = "Preço deve ser maior que zero")]
public decimal Price { get; set; }
```

### 2. Soft Delete
```csharp
public bool IsActive { get; set; } = true;

// Implementação de soft delete
public async Task<IActionResult> DeleteProduct(int id)
{
    var product = await _context.Products.FindAsync(id);
    if (product == null) return NotFound();
    
    product.IsActive = false; // Soft delete
    await _context.SaveChangesAsync();
    
    return NoContent();
}
```

### 3. Controle de Transações
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Operações do banco
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## 📈 Performance e Otimização

### 1. Paginação
```csharp
var products = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### 2. Projeções
```csharp
var productSummary = await _context.Products
    .Select(p => new { p.Id, p.Name, p.Price })
    .ToListAsync();
```

### 3. Índices no Banco
```csharp
modelBuilder.Entity<Product>()
    .HasIndex(p => p.Name)
    .HasDatabaseName("IX_Product_Name");
```

## 🧪 Estratégia de Testes

### 1. Testes de Unidade
- Testes dos controllers
- Testes dos serviços
- Testes das consultas LINQ

### 2. Testes de Integração
- Testes de API endpoints
- Testes de integração com banco de dados
- Testes de integração com APIs externas

### 3. Testes de Performance
- Testes de carga
- Testes de stress
- Monitoramento de consultas

---

Esta arquitetura garante **escalabilidade**, **manutenibilidade** e **testabilidade** do sistema, seguindo as melhores práticas de desenvolvimento .NET.