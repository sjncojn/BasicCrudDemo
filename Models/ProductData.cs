#pragma warning disable CS8603 // Possible null reference return.
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BasicCrudDemo.Models
{
    public class ProductsContext : DbContext
    {
        public DbSet<Product> Products { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<ProductStore> ProductStores { get; set; }

        public ProductsContext(DbContextOptions<ProductsContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductStore>()
                .HasKey(ps => new { ps.ProductId, ps.StoreId });

            modelBuilder.Entity<ProductStore>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductStores)
                .HasForeignKey(ps => ps.ProductId);

            modelBuilder.Entity<ProductStore>()
                .HasOne(ps => ps.Store)
                .WithMany(s => s.ProductStores)
                .HasForeignKey(ps => ps.StoreId);
        }



        public async Task<List<Product>> GetProductsAsync()
        {
            return await Products.FromSqlRaw("EXEC GetProducts").ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            var products = await Products.FromSqlRaw("EXEC GetProductById @ProductId = {0}", id).AsNoTracking().ToListAsync();
            return products.FirstOrDefault();
        }

        public async Task<int> AddProductAsync(Product product)
        {
            // Initialize the output parameter
            var productIdParam = new SqlParameter
            {
                ParameterName = "@ProductId",
                SqlDbType = System.Data.SqlDbType.Int,
                Direction = System.Data.ParameterDirection.Output
            };

            // Execute the stored procedure
            await Database.ExecuteSqlRawAsync(
                "EXEC AddProduct @Name, @Price, @Provider, @ProductId OUT",
                new SqlParameter("@Name", product.Name),
                new SqlParameter("@Price", product.Price),
                new SqlParameter("@Provider", product.Provider),
                productIdParam
            );

            // Get the output value
            return (int)productIdParam.Value;
        }

        public async Task UpdateProductAsync(Product product)
        {
            await Database.ExecuteSqlRawAsync(
                "EXEC UpdateProduct @ProductId = {0}, @Name = {1}, @Price = {2}, @Provider = {3}",
                product.ProductId, product.Name, product.Price, product.Provider);
        }

        public async Task DeleteProductAsync(int id)
        {
            await Database.ExecuteSqlRawAsync(
                "EXEC DeleteProduct @ProductId = {0}", id);
        }

        public async Task<List<Product>> SearchProductsAsync(string sortField, string sortOrder, string nameFilter, int pageNumber, int pageSize)
        {
            var products = await Products
                .FromSqlRaw("EXEC SearchProducts @SortField = {0}, @SortOrder = {1}, @NameFilter = {2}, @PageNumber = {3}, @PageSize = {4}",
                    sortField, sortOrder, nameFilter, pageNumber, pageSize)
                .ToListAsync();

            return products;
        }
    }
}
#pragma warning restore CS8603 // Possible null reference return.
