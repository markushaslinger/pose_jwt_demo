using UnionGen;
using UnionGen.Types;

namespace JwtDemo.Core.Products;

public partial interface IProductService
{
    public ValueTask<IReadOnlyCollection<Product>> GetAllProducts();
    public ValueTask<GetProductResult> GetProductById(int productId);
    public ValueTask<PriceUpdateResult> UpdateProductPrice(int productId, decimal requestPrice);

    [Union<Product, NotFound>]
    public readonly partial struct GetProductResult;

    [Union<Success, NotFound>]
    public readonly partial struct PriceUpdateResult;
}

// not really relevant for this demo - basic implementation for completeness
internal sealed class ProductService(IUnitOfWork unitOfWork) : IProductService
{
    public ValueTask<IReadOnlyCollection<Product>> GetAllProducts() => unitOfWork.ProductRepository.GetAllProducts();

    public async ValueTask<IProductService.GetProductResult> GetProductById(int productId)
    {
        var product = await unitOfWork.ProductRepository.GetById(productId);

        return product is not null
            ? product
            : new NotFound();
    }

    public async ValueTask<IProductService.PriceUpdateResult> UpdateProductPrice(int productId, decimal requestPrice)
    {
        var product = await unitOfWork.ProductRepository.GetById(productId);

        if (product is null)
        {
            return new NotFound();
        }

        product.Price = requestPrice;

        return new Success();
    }
}
