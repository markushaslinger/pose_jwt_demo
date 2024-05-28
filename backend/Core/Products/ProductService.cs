using UnionGen;
using UnionGen.Types;

namespace JwtDemo.Core.Products;

public partial interface IProductService
{
    // we fake it in-memory, so no async needed
    IReadOnlyCollection<Product> GetAllProducts();
    GetProductResult GetProductById(int productId);
    PriceUpdateResult UpdateProductPrice(int productId, decimal requestPrice);

    [Union<Product, NotFound>]
    public readonly partial struct GetProductResult;

    [Union<Success, NotFound>]
    public readonly partial struct PriceUpdateResult;
}

internal sealed class ProductService : IProductService
{
    // not really relevant for this demo, faked in-memory
    private static readonly List<Product> products =
    [
        new Product(1234, "Rice", 0.99M),
        new Product(1235, "Beans", 1.29M),
        new Product(1236, "Spaghetti", 1.50M),
        new Product(1237, "Bread", 2.2M),
        new Product(1238, "Bottled Water", 0.79M)
    ];

    public IReadOnlyCollection<Product> GetAllProducts() => products;

    public IProductService.GetProductResult GetProductById(int productId)
    {
        var product = FindProduct(productId);

        return product is not null
            ? product
            : new NotFound();
    }

    public IProductService.PriceUpdateResult UpdateProductPrice(int productId, decimal requestPrice)
    {
        var product = FindProduct(productId);
        
        if (product is null)
        {
            return new NotFound();
        }
        
        product.Price = requestPrice;

        return new Success();
    }
    
    private static Product? FindProduct(int productId) =>
        products.FirstOrDefault(p => p.Id == productId);
}
