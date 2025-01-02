using JwtDemo.Core;
using JwtDemo.Core.Auth;
using JwtDemo.Core.Products;
using JwtDemo.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtDemo.Controllers;

[ApiController]
[Authorize(nameof(UserRole.User))]
[Route("api/products")]
public sealed class ProductController(IProductService productService, ITransactionProvider transaction) : ControllerBase
{
    [HttpGet]
    public async ValueTask<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await productService.GetAllProducts();

        return Ok(products.Select(ToDto));
    }

    [HttpGet]
    [Route("{productId:int}")]
    public async ValueTask<ActionResult<ProductDto>> GetById([FromRoute] int productId)
    {
        var productResult = await productService.GetProductById(productId);

        return productResult
            .Match<ActionResult<ProductDto>>(product => Ok(product),
                                             _ => NotFound());
    }

    [HttpPatch]
    [Authorize(nameof(UserRole.Admin))]
    [Route("{productId:int}/price")]
    public async ValueTask<IActionResult> UpdatePrice([FromRoute] int productId,
                                                      [FromBody] ProductPriceUpdateRequest request)
    {
        try
        {
            await transaction.BeginTransactionAsync();

            // for this demo, we don't care about validation
            var updateResult = await productService.UpdateProductPrice(productId, request.Price);

            return await updateResult
                .MatchAsync<IActionResult>(async _ =>
                                           {
                                               await transaction.CommitAsync();

                                               return NoContent();
                                           },
                                           _ => ValueTask.FromResult<IActionResult>(NotFound()));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine(ex.Message);

            return Problem();
        }
    }

    private static ProductDto ToDto(Product product) =>
        new()
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price
        };
}
