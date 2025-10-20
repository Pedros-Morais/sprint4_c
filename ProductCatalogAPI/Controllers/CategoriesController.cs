using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Data;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém todas as categorias
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories
                .Where(c => c.IsActive)
                .Include(c => c.Products.Where(p => p.IsActive))
                .ToListAsync();
        }

        /// <summary>
        /// Obtém uma categoria específica por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products.Where(p => p.IsActive))
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (category == null)
            {
                return NotFound();
            }

            return category;
        }

        /// <summary>
        /// Obtém estatísticas de uma categoria
        /// </summary>
        [HttpGet("{id}/stats")]
        public async Task<ActionResult<object>> GetCategoryStats(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products.Where(p => p.IsActive))
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (category == null)
            {
                return NotFound();
            }

            var stats = new
            {
                CategoryName = category.Name,
                TotalProducts = category.Products.Count,
                AveragePrice = category.Products.Any() ? category.Products.Average(p => p.Price) : 0,
                TotalStock = category.Products.Sum(p => p.Stock),
                AverageRating = category.Products.Where(p => p.Rating.HasValue).Any() 
                    ? category.Products.Where(p => p.Rating.HasValue).Average(p => p.Rating!.Value) 
                    : 0,
                TopBrands = category.Products
                    .Where(p => !string.IsNullOrEmpty(p.Brand))
                    .GroupBy(p => p.Brand)
                    .Select(g => new { Brand = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList()
            };

            return stats;
        }

        /// <summary>
        /// Cria uma nova categoria
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            category.CreatedAt = DateTime.UtcNow;
            
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCategory", new { id = category.Id }, category);
        }

        /// <summary>
        /// Atualiza uma categoria existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category category)
        {
            if (id != category.Id)
            {
                return BadRequest();
            }

            var existingCategory = await _context.Categories.FindAsync(id);
            if (existingCategory == null)
            {
                return NotFound();
            }

            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            existingCategory.IsActive = category.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
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
        /// Remove uma categoria (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // Verificar se há produtos associados
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id && p.IsActive);
            if (hasProducts)
            {
                return BadRequest("Não é possível excluir uma categoria que possui produtos associados.");
            }

            category.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id);
        }
    }
}