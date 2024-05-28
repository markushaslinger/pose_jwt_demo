namespace JwtDemo.Core.Products;

public sealed class Product(int id, string name, decimal price)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public decimal Price { get; set; } = price;
} 
