using Microsoft.EntityFrameworkCore;
using ProductCatalogAPI.Models;

namespace ProductCatalogAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurações de relacionamentos
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed data
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Eletrônicos", Description = "Produtos eletrônicos em geral" },
                new Category { Id = 2, Name = "Roupas", Description = "Vestuário e acessórios" },
                new Category { Id = 3, Name = "Casa e Jardim", Description = "Produtos para casa e jardim" },
                new Category { Id = 4, Name = "Livros", Description = "Livros e materiais educativos" },
                new Category { Id = 5, Name = "Esportes", Description = "Equipamentos esportivos" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Smartphone Samsung Galaxy", Description = "Smartphone com 128GB", Price = 1299.99m, Stock = 50, CategoryId = 1, Brand = "Samsung", Rating = 4.5 },
                new Product { Id = 2, Name = "Notebook Dell Inspiron", Description = "Notebook 15.6 polegadas", Price = 2499.99m, Stock = 25, CategoryId = 1, Brand = "Dell", Rating = 4.2 },
                new Product { Id = 3, Name = "Camiseta Polo", Description = "Camiseta polo masculina", Price = 89.99m, Stock = 100, CategoryId = 2, Brand = "Lacoste", Rating = 4.0 },
                new Product { Id = 4, Name = "Tênis Nike Air Max", Description = "Tênis esportivo Nike", Price = 399.99m, Stock = 75, CategoryId = 5, Brand = "Nike", Rating = 4.7 },
                new Product { Id = 5, Name = "Livro Clean Code", Description = "Livro sobre código limpo", Price = 79.99m, Stock = 30, CategoryId = 4, Brand = "Pearson", Rating = 4.8 }
            );

            modelBuilder.Entity<Customer>().HasData(
                new Customer { Id = 1, Name = "João Silva", Email = "joao@email.com", Phone = "11999999999", City = "São Paulo" },
                new Customer { Id = 2, Name = "Maria Santos", Email = "maria@email.com", Phone = "11888888888", City = "Rio de Janeiro" },
                new Customer { Id = 3, Name = "Pedro Oliveira", Email = "pedro@email.com", Phone = "11777777777", City = "Belo Horizonte" }
            );
        }
    }
}