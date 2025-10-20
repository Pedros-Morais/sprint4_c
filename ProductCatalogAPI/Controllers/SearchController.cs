using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;
using System.Linq.Expressions;

namespace ProductCatalogAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Pesquisa avançada de produtos com múltiplos filtros
        /// </summary>
        [HttpGet("products/advanced")]
        public async Task<ActionResult<object>> AdvancedProductSearch(
            [FromQuery] string? name,
            [FromQuery] string? description,
            [FromQuery] string? brand,
            [FromQuery] int? categoryId,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int? minStock,
            [FromQuery] double? minRating,
            [FromQuery] bool? inStock,
            [FromQuery] DateTime? createdAfter,
            [FromQuery] DateTime? createdBefore,
            [FromQuery] string sortBy = "name",
            [FromQuery] string sortOrder = "asc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            // Aplicar filtros usando LINQ
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(p => p.Name.ToLower().Contains(name.ToLower()));
            }

            if (!string.IsNullOrEmpty(description))
            {
                query = query.Where(p => p.Description != null && 
                    p.Description.ToLower().Contains(description.ToLower()));
            }

            if (!string.IsNullOrEmpty(brand))
            {
                query = query.Where(p => p.Brand != null && 
                    p.Brand.ToLower().Contains(brand.ToLower()));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (minStock.HasValue)
            {
                query = query.Where(p => p.Stock >= minStock.Value);
            }

            if (minRating.HasValue)
            {
                query = query.Where(p => p.Rating.HasValue && p.Rating >= minRating.Value);
            }

            if (inStock.HasValue)
            {
                if (inStock.Value)
                {
                    query = query.Where(p => p.Stock > 0);
                }
                else
                {
                    query = query.Where(p => p.Stock == 0);
                }
            }

            if (createdAfter.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= createdAfter.Value);
            }

            if (createdBefore.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= createdBefore.Value);
            }

            // Aplicar ordenação
            query = ApplyProductSorting(query, sortBy, sortOrder);

            // Contar total antes da paginação
            var totalCount = await query.CountAsync();

            // Aplicar paginação
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Stock,
                    p.Brand,
                    p.Rating,
                    p.ImageUrl,
                    p.CreatedAt,
                    Category = new { p.Category.Id, p.Category.Name }
                })
                .ToListAsync();

            var result = new
            {
                Products = products,
                Pagination = new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                },
                Filters = new
                {
                    Name = name,
                    Description = description,
                    Brand = brand,
                    CategoryId = categoryId,
                    MinPrice = minPrice,
                    MaxPrice = maxPrice,
                    MinStock = minStock,
                    MinRating = minRating,
                    InStock = inStock,
                    CreatedAfter = createdAfter,
                    CreatedBefore = createdBefore,
                    SortBy = sortBy,
                    SortOrder = sortOrder
                }
            };

            return Ok(result);
        }

        /// <summary>
        /// Pesquisa de produtos similares baseada em características
        /// </summary>
        [HttpGet("products/{id}/similar")]
        public async Task<ActionResult<object>> GetSimilarProducts(int id, [FromQuery] int limit = 5)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound("Produto não encontrado");
            }

            // Buscar produtos similares usando LINQ complexo
            var similarProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Id != id)
                .Where(p => 
                    // Mesma categoria
                    p.CategoryId == product.CategoryId ||
                    // Mesma marca
                    (p.Brand != null && product.Brand != null && p.Brand == product.Brand) ||
                    // Faixa de preço similar (±20%)
                    (p.Price >= product.Price * 0.8m && p.Price <= product.Price * 1.2m))
                .OrderBy(p => 
                    // Priorizar por categoria, depois por marca, depois por preço
                    p.CategoryId == product.CategoryId ? 0 : 1)
                .ThenBy(p => 
                    p.Brand == product.Brand ? 0 : 1)
                .ThenBy(p => 
                    Math.Abs(p.Price - product.Price))
                .Take(limit)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Brand,
                    p.Rating,
                    p.ImageUrl,
                    Category = new { p.Category.Id, p.Category.Name },
                    SimilarityReasons = new List<string?>
                    {
                        p.CategoryId == product.CategoryId ? "Mesma categoria" : null,
                        p.Brand == product.Brand ? "Mesma marca" : null,
                        (p.Price >= product.Price * 0.8m && p.Price <= product.Price * 1.2m) ? "Preço similar" : null
                    }.Where(r => r != null).Cast<string>().ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                OriginalProduct = new
                {
                    product.Id,
                    product.Name,
                    product.Price,
                    product.Brand,
                    Category = new { product.Category.Id, product.Category.Name }
                },
                SimilarProducts = similarProducts
            });
        }

        /// <summary>
        /// Pesquisa de clientes com análise de comportamento
        /// </summary>
        [HttpGet("customers/behavior")]
        public async Task<ActionResult<object>> GetCustomerBehaviorAnalysis(
            [FromQuery] string? city,
            [FromQuery] decimal? minSpent,
            [FromQuery] int? minOrders,
            [FromQuery] DateTime? registeredAfter,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Customers
                .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(c => c.IsActive)
                .AsQueryable();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(c => c.City != null && 
                    c.City.ToLower().Contains(city.ToLower()));
            }

            if (registeredAfter.HasValue)
            {
                query = query.Where(c => c.CreatedAt >= registeredAfter.Value);
            }

            // Análise comportamental usando LINQ complexo
            var customerAnalysis = await query
                .Select(c => new
                {
                    Customer = new
                    {
                        c.Id,
                        c.Name,
                        c.Email,
                        c.City,
                        c.CreatedAt
                    },
                    Behavior = new
                    {
                        TotalOrders = c.Orders.Count(),
                        TotalSpent = c.Orders.Sum(o => o.TotalAmount),
                        AverageOrderValue = c.Orders.Any() ? c.Orders.Average(o => o.TotalAmount) : 0,
                        LastOrderDate = c.Orders.Any() ? c.Orders.Max(o => o.OrderDate) : (DateTime?)null,
                        FavoriteCategories = c.Orders
                            .SelectMany(o => o.OrderItems)
                            .GroupBy(oi => oi.Product.Category.Name)
                            .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                            .Take(3)
                            .Select(g => new { Category = g.Key, Quantity = g.Sum(oi => oi.Quantity) })
                            .ToList(),
                        MostBoughtProducts = c.Orders
                            .SelectMany(o => o.OrderItems)
                            .GroupBy(oi => new { oi.Product.Id, oi.Product.Name })
                            .OrderByDescending(g => g.Sum(oi => oi.Quantity))
                            .Take(3)
                            .Select(g => new { 
                                ProductId = g.Key.Id, 
                                ProductName = g.Key.Name, 
                                Quantity = g.Sum(oi => oi.Quantity) 
                            })
                            .ToList(),
                        CustomerSegment = 
                            c.Orders.Sum(o => o.TotalAmount) > 1000 ? "Premium" :
                            c.Orders.Sum(o => o.TotalAmount) > 500 ? "Regular" : "Basic",
                        DaysSinceLastOrder = c.Orders.Any() ? 
                            (DateTime.UtcNow - c.Orders.Max(o => o.OrderDate)).Days : 
                            (DateTime.UtcNow - c.CreatedAt).Days
                    }
                })
                .Where(ca => 
                    (!minSpent.HasValue || ca.Behavior.TotalSpent >= minSpent.Value) &&
                    (!minOrders.HasValue || ca.Behavior.TotalOrders >= minOrders.Value))
                .OrderByDescending(ca => ca.Behavior.TotalSpent)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await query.CountAsync();

            return Ok(new
            {
                CustomerAnalysis = customerAnalysis,
                Pagination = new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }

        /// <summary>
        /// Análise de padrões de compra por período
        /// </summary>
        [HttpGet("purchase-patterns")]
        public async Task<ActionResult<object>> GetPurchasePatterns(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.AddMonths(-6);
            var end = endDate ?? DateTime.UtcNow;

            // Análise complexa de padrões usando LINQ
            var patterns = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .GroupBy(o => new
                {
                    Year = o.OrderDate.Year,
                    Month = o.OrderDate.Month,
                    DayOfWeek = (int)o.OrderDate.DayOfWeek,
                    Hour = o.OrderDate.Hour
                })
                .Select(g => new
                {
                    Period = new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        DayOfWeek = ((DayOfWeek)g.Key.DayOfWeek).ToString(),
                        g.Key.Hour
                    },
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.TotalAmount),
                    AverageOrderValue = g.Average(o => o.TotalAmount),
                    UniqueCustomers = g.Select(o => o.CustomerId).Distinct().Count(),
                    TopCategories = g.SelectMany(o => o.OrderItems)
                        .GroupBy(oi => oi.Product.Category.Name)
                        .OrderByDescending(cg => cg.Sum(oi => oi.Quantity))
                        .Take(3)
                        .Select(cg => new { Category = cg.Key, Quantity = cg.Sum(oi => oi.Quantity) })
                        .ToList()
                })
                .OrderBy(p => p.Period.Year)
                .ThenBy(p => p.Period.Month)
                .ThenBy(p => p.Period.DayOfWeek)
                .ThenBy(p => p.Period.Hour)
                .ToListAsync();

            // Análise de sazonalidade
            var seasonalAnalysis = patterns
                .GroupBy(p => new { p.Period.Month })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    MonthName = new DateTime(2024, g.Key.Month, 1).ToString("MMMM"),
                    AverageOrders = g.Average(p => p.OrderCount),
                    AverageRevenue = g.Average(p => p.TotalRevenue),
                    TotalOrders = g.Sum(p => p.OrderCount),
                    TotalRevenue = g.Sum(p => p.TotalRevenue)
                })
                .OrderBy(s => s.Month)
                .ToList();

            // Análise por dia da semana
            var weekdayAnalysis = patterns
                .GroupBy(p => p.Period.DayOfWeek)
                .Select(g => new
                {
                    DayOfWeek = g.Key,
                    AverageOrders = g.Average(p => p.OrderCount),
                    AverageRevenue = g.Average(p => p.TotalRevenue),
                    TotalOrders = g.Sum(p => p.OrderCount),
                    TotalRevenue = g.Sum(p => p.TotalRevenue)
                })
                .OrderBy(w => w.DayOfWeek)
                .ToList();

            // Análise por hora do dia
            var hourlyAnalysis = patterns
                .GroupBy(p => p.Period.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    AverageOrders = g.Average(p => p.OrderCount),
                    AverageRevenue = g.Average(p => p.TotalRevenue),
                    TotalOrders = g.Sum(p => p.OrderCount),
                    TotalRevenue = g.Sum(p => p.TotalRevenue)
                })
                .OrderBy(h => h.Hour)
                .ToList();

            return Ok(new
            {
                Period = new { StartDate = start, EndDate = end },
                DetailedPatterns = patterns,
                SeasonalAnalysis = seasonalAnalysis,
                WeekdayAnalysis = weekdayAnalysis,
                HourlyAnalysis = hourlyAnalysis,
                Summary = new
                {
                    BestMonth = seasonalAnalysis.OrderByDescending(s => s.TotalRevenue).FirstOrDefault(),
                    BestDayOfWeek = weekdayAnalysis.OrderByDescending(w => w.TotalRevenue).FirstOrDefault(),
                    BestHour = hourlyAnalysis.OrderByDescending(h => h.TotalRevenue).FirstOrDefault()
                }
            });
        }

        private IQueryable<Product> ApplyProductSorting(IQueryable<Product> query, string sortBy, string sortOrder)
        {
            var isDescending = sortOrder.ToLower() == "desc";

            return sortBy.ToLower() switch
            {
                "name" => isDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "price" => isDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "stock" => isDescending ? query.OrderByDescending(p => p.Stock) : query.OrderBy(p => p.Stock),
                "rating" => isDescending ? query.OrderByDescending(p => p.Rating) : query.OrderBy(p => p.Rating),
                "created" => isDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                "brand" => isDescending ? query.OrderByDescending(p => p.Brand) : query.OrderBy(p => p.Brand),
                _ => query.OrderBy(p => p.Name)
            };
        }
    }
}