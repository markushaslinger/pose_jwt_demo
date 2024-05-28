using UnionGen;
using UnionGen.Types;

namespace JwtDemo.Core.Products;

public partial interface IProductService
{
    ValueTask<IReadOnlyCollection<Product>> GetAllProducts();
    ValueTask<GetProductResult> GetProductById(int productId);
    ValueTask<PriceUpdateResult> UpdateProductPrice(int productId, decimal requestPrice);

    [Union<Product, NotFound>]
    public readonly partial struct GetProductResult;

    [Union<Success, NotFound>]
    public readonly partial struct PriceUpdateResult;
}

// not really relevant for this demo - basic implementation for completeness
internal sealed class ProductService(IUnitOfWork unitOfWork) : IProductService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    
    public ValueTask<IReadOnlyCollection<Product>> GetAllProducts() => _unitOfWork.ProductRepository.GetAllProducts();

    public async ValueTask<IProductService.GetProductResult> GetProductById(int productId)
    {
        var product = await _unitOfWork.ProductRepository.GetById(productId);

        return product is not null
            ? product
            : new NotFound();
    }

    public async ValueTask<IProductService.PriceUpdateResult> UpdateProductPrice(int productId, decimal requestPrice)
    {
        await _unitOfWork.BeginTransaction();
        try
        {
            var product = await _unitOfWork.ProductRepository.GetById(productId);

            if (product is null)
            {
                return new NotFound();
            }

            product.Price = requestPrice;

            await _unitOfWork.Commit();

            return new Success();
        }
        catch (Exception ex)
        {
            await _unitOfWork.Rollback();
            
            // no logging configured in this auth demo project
            Console.WriteLine(ex.Message);

            throw;
        }
    }
}
