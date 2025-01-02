using JwtDemo.Core.Auth;

namespace JwtDemo.Core.Users;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required byte[] PasswordHash { get; set; }
    public required byte[] PasswordSalt { get; set; }
    public required UserRole Role { get; set; }
    
    // storing those in something like a HybridCache (maybe ValKey backed) with automatic eviction would be a good idea
    // but to keep things simple, we'll just store them in the database
    public required List<ActiveRefreshToken> ActiveRefreshTokens { get; set; } = [];
}
