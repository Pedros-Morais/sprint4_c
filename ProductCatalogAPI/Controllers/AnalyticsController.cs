using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Dashboard geral com estatísticas principais
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetDashboard()
        {
            var totalProducts = await _context.Products.CountAsync(p => p.IsActive);
            var totalCategories = await _context.Categories.CountAsync(c => c.IsActive);
            var totalCustomers = await _context.Customers.CountAsync(c => c.IsActive);
            var totalOrders = await _context.Orders.CountAsync();

            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
            var averageOrderValue = await _context.Orders.AverageAsync(o => o.TotalAmount);

            var lowStockProducts = await _context.Products
                .Where(p => p.IsActive && p.Stock <= 10)
                .CountAsync();

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Pending");

            var dashboard = new
            {
                Summary = new
                {
                    TotalProducts = totalProducts,
                    TotalCategories = totalCategories,
                    TotalCustomers = totalCustomers,
                    TotalOrders = totalOrders,
                    TotalRevenue = totalRevenue,
                    AverageOrderValue = Math.Round(averageOrderValue, 2),
                    LowStockProducts = lowStockProducts,
                    PendingOrders = pendingOrders
                },
                RecentActivity = new
                {
                    RecentOrders = await _context.Orders
                        .Include(o => o.Customer)
                        .OrderByDescending(o => o.OrderDate)
                        .Take(5)
                        .Select(o => new { o.Id, o.Customer.Name, o.TotalAmount, o.OrderDate, o.Status })
                        .ToListAsync(),
                    
                    NewCustomers = await _context.Customers
                        .Where(c => c.IsActive)
                        .OrderByDescending(c => c.CreatedAt)
                        .Take(5)
                        .Select(c => new { c.Id, c.Name, c.Email, c.CreatedAt })
                        .ToListAsync()
                }
            };

            return Ok(dashboard);
        }

        /// <summary>
        /// Análise de produtos mais vendidos
        /// </summary>
        [HttpGet("top-selling-products")]
        public async Task<ActionResult<object>> GetTopSellingProducts([FromQuery] int top = 10)
        {
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Where(oi => oi.Product.IsActive)
                .GroupBy(oi => new { oi.ProductId, ProductName = oi.Product.Name, CategoryName = oi.Product.Category.Name })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName,
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.TotalPrice),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(top)
                .ToListAsync();

            return Ok(topProducts);
        }

        /// <summary>
        /// Análise de vendas por categoria
        /// </summary>
        [HttpGet("sales-by-category")]
        public async Task<ActionResult<object>> GetSalesByCategory()
        {
            var salesByCategory = await _context.OrderItems
                .Include(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .Where(oi => oi.Product.IsActive)
                .GroupBy(oi => oi.Product.Category.Name)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.TotalPrice),
                    ProductCount = g.Select(x => x.ProductId).Distinct().Count(),
                    AveragePrice = g.Average(x => x.UnitPrice)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            return Ok(salesByCategory);
        }

        /// <summary>
        /// Análise de clientes mais valiosos
        /// </summary>
        [HttpGet("top-customers")]
        public async Task<ActionResult<object>> GetTopCustomers([FromQuery] int top = 10)
        {
            var topCustomers = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Customer.IsActive)
                .GroupBy(o => new { o.CustomerId, o.Customer.Name, o.Customer.Email })
                .Select(g => new
                {
                    CustomerId = g.Key.CustomerId,
                    CustomerName = g.Key.Name,
                    CustomerEmail = g.Key.Email,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalAmount),
                    AverageOrderValue = g.Average(x => x.TotalAmount),
                    LastOrderDate = g.Max(x => x.OrderDate)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(top)
                .ToListAsync();

            return Ok(topCustomers);
        }

        /// <summary>
        /// Análise de vendas por período (mensal)
        /// </summary>
        [HttpGet("monthly-sales")]
        public async Task<ActionResult<object>> GetMonthlySales([FromQuery] int months = 12)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);

            var monthlySales = await _context.Orders
                .Where(o => o.OrderDate >= startDate)
                .GroupBy(o => new { Year = o.OrderDate.Year, Month = o.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                    AverageOrderValue = g.Average(x => x.TotalAmount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return Ok(monthlySales);
        }

        /// <summary>
        /// Análise de produtos por faixa de preço
        /// </summary>
        [HttpGet("products-by-price-range")]
        public async Task<ActionResult<object>> GetProductsByPriceRange()
        {
            var priceRanges = await _context.Products
                .Where(p => p.IsActive)
                .GroupBy(p => 
                    p.Price < 50 ? "0-50" :
                    p.Price < 100 ? "50-100" :
                    p.Price < 200 ? "100-200" :
                    p.Price < 500 ? "200-500" :
                    p.Price < 1000 ? "500-1000" : "1000+")
                .Select(g => new
                {
                    PriceRange = g.Key,
                    ProductCount = g.Count(),
                    AveragePrice = g.Average(x => x.Price),
                    TotalStock = g.Sum(x => x.Stock)
                })
                .OrderBy(x => x.PriceRange)
                .ToListAsync();

            return Ok(priceRanges);
        }

        /// <summary>
        /// Análise de estoque por categoria
        /// </summary>
        [HttpGet("stock-by-category")]
        public async Task<ActionResult<object>> GetStockByCategory()
        {
            var stockByCategory = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .GroupBy(p => p.Category.Name)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    TotalProducts = g.Count(),
                    TotalStock = g.Sum(x => x.Stock),
                    AverageStock = g.Average(x => x.Stock),
                    LowStockProducts = g.Count(x => x.Stock <= 10),
                    OutOfStockProducts = g.Count(x => x.Stock == 0),
                    TotalValue = g.Sum(x => x.Price * x.Stock)
                })
                .OrderByDescending(x => x.TotalValue)
                .ToListAsync();

            return Ok(stockByCategory);
        }

        /// <summary>
        /// Análise de tendências de vendas
        /// </summary>
        [HttpGet("sales-trends")]
        public async Task<ActionResult<object>> GetSalesTrends([FromQuery] int days = 30)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var dailySales = await _context.Orders
                .Where(o => o.OrderDate >= startDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalOrders = g.Count(),
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                    UniqueCustomers = g.Select(x => x.CustomerId).Distinct().Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var trends = new
            {
                Period = new { StartDate = startDate, EndDate = DateTime.UtcNow, Days = days },
                DailySales = dailySales,
                Summary = new
                {
                    TotalRevenue = dailySales.Sum(x => x.TotalRevenue),
                    TotalOrders = dailySales.Sum(x => x.TotalOrders),
                    AverageDailyRevenue = dailySales.Any() ? dailySales.Average(x => x.TotalRevenue) : 0,
                    AverageDailyOrders = dailySales.Any() ? dailySales.Average(x => x.TotalOrders) : 0,
                    BestDay = dailySales.OrderByDescending(x => x.TotalRevenue).FirstOrDefault(),
                    WorstDay = dailySales.OrderBy(x => x.TotalRevenue).FirstOrDefault()
                }
            };

            return Ok(trends);
        }

        /// <summary>
        /// Análise de produtos com melhor avaliação
        /// </summary>
        [HttpGet("top-rated-products")]
        public async Task<ActionResult<object>> GetTopRatedProducts([FromQuery] int top = 10)
        {
            var topRatedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.Rating.HasValue)
                .OrderByDescending(p => p.Rating)
                .ThenByDescending(p => p.CreatedAt)
                .Take(top)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Rating,
                    p.Brand,
                    CategoryName = p.Category.Name,
                    p.Stock
                })
                .ToListAsync();

            return Ok(topRatedProducts);
        }

        /// <summary>
        /// Análise de performance por marca
        /// </summary>
        [HttpGet("brand-performance")]
        public async Task<ActionResult<object>> GetBrandPerformance()
        {
            var brandPerformance = await _context.Products
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.Brand))
                .GroupBy(p => p.Brand)
                .Select(g => new
                {
                    Brand = g.Key,
                    ProductCount = g.Count(),
                    AveragePrice = g.Average(x => x.Price),
                    AverageRating = g.Where(x => x.Rating.HasValue).Any() 
                        ? g.Where(x => x.Rating.HasValue).Average(x => x.Rating!.Value) 
                        : 0,
                    TotalStock = g.Sum(x => x.Stock),
                    PriceRange = new
                    {
                        Min = g.Min(x => x.Price),
                        Max = g.Max(x => x.Price)
                    }
                })
                .OrderByDescending(x => x.ProductCount)
                .ToListAsync();

            return Ok(brandPerformance);
        }
    }
}