using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém todos os clientes
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            return await _context.Customers
                .Where(c => c.IsActive)
                .Include(c => c.Orders)
                .ToListAsync();
        }

        /// <summary>
        /// Obtém um cliente específico por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (customer == null)
            {
                return NotFound();
            }

            return customer;
        }

        /// <summary>
        /// Pesquisa clientes por nome ou email
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Customer>>> SearchCustomers([FromQuery] string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return await GetCustomers();
            }

            var customers = await _context.Customers
                .Where(c => c.IsActive && 
                    (c.Name.Contains(query) || c.Email.Contains(query)))
                .Include(c => c.Orders)
                .ToListAsync();

            return customers;
        }

        /// <summary>
        /// Obtém clientes por cidade
        /// </summary>
        [HttpGet("by-city/{city}")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomersByCity(string city)
        {
            var customers = await _context.Customers
                .Where(c => c.IsActive && c.City != null && c.City.Contains(city))
                .Include(c => c.Orders)
                .ToListAsync();

            return customers;
        }

        /// <summary>
        /// Obtém estatísticas de um cliente
        /// </summary>
        [HttpGet("{id}/stats")]
        public async Task<ActionResult<object>> GetCustomerStats(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (customer == null)
            {
                return NotFound();
            }

            var stats = new
            {
                CustomerName = customer.Name,
                TotalOrders = customer.Orders.Count,
                TotalSpent = customer.Orders.Sum(o => o.TotalAmount),
                AverageOrderValue = customer.Orders.Any() ? customer.Orders.Average(o => o.TotalAmount) : 0,
                LastOrderDate = customer.Orders.Any() ? customer.Orders.Max(o => o.OrderDate) : (DateTime?)null,
                FavoriteProducts = customer.Orders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.Product.Name)
                    .Select(g => new { ProductName = g.Key, Quantity = g.Sum(x => x.Quantity) })
                    .OrderByDescending(x => x.Quantity)
                    .Take(5)
                    .ToList()
            };

            return stats;
        }

        /// <summary>
        /// Cria um novo cliente
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            // Verificar se já existe um cliente com o mesmo email
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == customer.Email && c.IsActive);

            if (existingCustomer != null)
            {
                return BadRequest("Já existe um cliente cadastrado com este email.");
            }

            customer.CreatedAt = DateTime.UtcNow;
            
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCustomer", new { id = customer.Id }, customer);
        }

        /// <summary>
        /// Atualiza um cliente existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.Id)
            {
                return BadRequest();
            }

            var existingCustomer = await _context.Customers.FindAsync(id);
            if (existingCustomer == null)
            {
                return NotFound();
            }

            // Verificar se o email já está sendo usado por outro cliente
            var emailExists = await _context.Customers
                .AnyAsync(c => c.Email == customer.Email && c.Id != id && c.IsActive);

            if (emailExists)
            {
                return BadRequest("Este email já está sendo usado por outro cliente.");
            }

            existingCustomer.Name = customer.Name;
            existingCustomer.Email = customer.Email;
            existingCustomer.Phone = customer.Phone;
            existingCustomer.Address = customer.Address;
            existingCustomer.City = customer.City;
            existingCustomer.PostalCode = customer.PostalCode;
            existingCustomer.IsActive = customer.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>
        /// Remove um cliente (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}