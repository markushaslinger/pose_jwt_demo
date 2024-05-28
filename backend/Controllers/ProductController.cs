using JwtDemo.Core.Auth;
using JwtDemo.Core.Products;
using JwtDemo.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtDemo.Controllers;

[ApiController]
[Authorize(nameof(UserRole.User))]
[Route("api/products")]
public sealed class ProductController(IProductService productService) : ControllerBase
{
    private readonly IProductService _productService = productService;

    [HttpGet]
    public async ValueTask<ActionResult<IEnumerable<ProductDto>>> GetAll()
    {
        var products = await _productService.GetAllProducts();

        return Ok(products.Select(ToDto));
    }

    [HttpGet]
    [Route("{productId:int}")]
    public async ValueTask<ActionResult<ProductDto>> GetById([FromRoute] int productId)
    {
        var productResult = await _productService.GetProductById(productId);

        return productResult
            .Match<ActionResult<ProductDto>>(product => Ok(product),
                                             _ => NotFound());
    }

    [HttpPatch]
    [Authorize(nameof(UserRole.Admin))]
    [Route("{productId:int}/price")]
    public async ValueTask<IActionResult> UpdatePrice([FromRoute] int productId, [FromBody] ProductPriceUpdateRequest request)
    {
        // for this demo, we don't care about UOW/transactions
        // we also don't care about validation this time
        var updateResult = await _productService.UpdateProductPrice(productId, request.Price);

        return updateResult
            .Match<IActionResult>(_ => NoContent(),
                                  _ => NotFound());
    }

    private static ProductDto ToDto(Product product) =>
        new()
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price
        };
}
