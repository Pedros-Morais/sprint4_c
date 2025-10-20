using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém todos os pedidos
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        /// <summary>
        /// Obtém um pedido específico por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }

        /// <summary>
        /// Obtém pedidos por cliente
        /// </summary>
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByCustomer(int customerId)
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders;
        }

        /// <summary>
        /// Obtém pedidos por status
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByStatus(string status)
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.Status.ToLower() == status.ToLower())
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders;
        }

        /// <summary>
        /// Obtém pedidos por período
        /// </summary>
        [HttpGet("period")]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrdersByPeriod(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return orders;
        }

        /// <summary>
        /// Obtém relatório de vendas
        /// </summary>
        [HttpGet("sales-report")]
        public async Task<ActionResult<object>> GetSalesReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.Orders.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(o => o.OrderDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(o => o.OrderDate <= endDate.Value);

            var orders = await query
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.Category)
                .ToListAsync();

            var report = new
            {
                Period = new { StartDate = startDate, EndDate = endDate },
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0,
                OrdersByStatus = orders.GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count(), Revenue = g.Sum(x => x.TotalAmount) })
                    .ToList(),
                TopProducts = orders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.Product.Name)
                    .Select(g => new { 
                        ProductName = g.Key, 
                        Quantity = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.TotalPrice)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(10)
                    .ToList(),
                TopCategories = orders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.Product.Category.Name)
                    .Select(g => new { 
                        CategoryName = g.Key, 
                        Quantity = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.TotalPrice)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToList()
            };

            return report;
        }

        /// <summary>
        /// Cria um novo pedido
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(CreateOrderRequest request)
        {
            // Verificar se o cliente existe
            var customer = await _context.Customers.FindAsync(request.CustomerId);
            if (customer == null)
            {
                return BadRequest("Cliente não encontrado.");
            }

            var order = new Order
            {
                CustomerId = request.CustomerId,
                OrderDate = DateTime.UtcNow,
                Status = "Pending",
                Notes = request.Notes
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            decimal totalAmount = 0;

            foreach (var item in request.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    return BadRequest($"Produto com ID {item.ProductId} não encontrado.");
                }

                if (product.Stock < item.Quantity)
                {
                    return BadRequest($"Estoque insuficiente para o produto {product.Name}. Disponível: {product.Stock}");
                }

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };

                _context.OrderItems.Add(orderItem);
                totalAmount += orderItem.TotalPrice;

                // Atualizar estoque
                product.Stock -= item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }

            order.TotalAmount = totalAmount;
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOrder", new { id = order.Id }, order);
        }

        /// <summary>
        /// Atualiza o status de um pedido
        /// </summary>
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                return BadRequest("Status inválido. Valores válidos: " + string.Join(", ", validStatuses));
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Cancela um pedido
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status == "Delivered" || order.Status == "Cancelled")
            {
                return BadRequest("Não é possível cancelar um pedido que já foi entregue ou cancelado.");
            }

            // Restaurar estoque
            foreach (var item in order.OrderItems)
            {
                item.Product.Stock += item.Quantity;
                item.Product.UpdatedAt = DateTime.UtcNow;
            }

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }

    public class CreateOrderRequest
    {
        public int CustomerId { get; set; }
        public string? Notes { get; set; }
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}