namespace JwtDemo.Model;

public sealed class ProductDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
}

public sealed class ProductPriceUpdateRequest
{
    public decimal Price { get; set; }
}