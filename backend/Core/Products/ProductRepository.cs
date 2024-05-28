using Microsoft.EntityFrameworkCore;

namespace JwtDemo.Core.Products;

public interface IProductRepository
{
    ValueTask<IReadOnlyCollection<Product>> GetAllProducts();
    ValueTask<Product?> GetById(int productId);
}

public sealed class ProductRepository(DbSet<Product> products) : IProductRepository
{
    private readonly DbSet<Product> _products = products;

    public async ValueTask<IReadOnlyCollection<Product>> GetAllProducts()
    {
        var products = await _products.ToListAsync();

        return products;
    }

    public async ValueTask<Product?> GetById(int productId)
    {
        var product = await _products.FindAsync(productId);

        return product;
    }
}
