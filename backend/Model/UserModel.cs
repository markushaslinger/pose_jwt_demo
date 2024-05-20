using JwtDemo.Core.Auth;

namespace JwtDemo.Model;

public sealed class RegisterRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required UserRole Role { get; set; }
}
