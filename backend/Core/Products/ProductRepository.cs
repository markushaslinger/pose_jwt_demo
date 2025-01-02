using Microsoft.EntityFrameworkCore;

namespace JwtDemo.Core.Products;

public interface IProductRepository
{
    public ValueTask<IReadOnlyCollection<Product>> GetAllProducts();
    public ValueTask<Product?> GetById(int productId);
}

public sealed class ProductRepository(DbSet<Product> products) : IProductRepository
{
    public async ValueTask<IReadOnlyCollection<Product>> GetAllProducts() => await products.ToListAsync();

    public async ValueTask<Product?> GetById(int productId)
    {
        var product = await products.FindAsync(productId);

        return product;
    }
}
