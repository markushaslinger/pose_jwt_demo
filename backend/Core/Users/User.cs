using JwtDemo.Core.Auth;

namespace JwtDemo.Core.Users;

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required byte[] PasswordHash { get; set; }
    public required byte[] PasswordSalt { get; set; }
    public required UserRole Role { get; set; }
    public required List<ActiveRefreshToken> ActiveRefreshTokens { get; set; }
}
