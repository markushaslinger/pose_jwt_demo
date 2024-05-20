using JwtDemo.Core.Auth;
using NodaTime;

namespace JwtDemo.Model;

public sealed class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

public sealed class TokenResponse
{
    public required TokenDataDto AccessToken { get; set; }
    public required TokenDataDto RefreshToken { get; set; }
}

public sealed class TokenRefreshRequest
{
    public required string Username { get; set; }
    public required string RefreshToken { get; set; }
}

public sealed class TokenDataDto
{
    public required string Token { get; set; }
    public required Instant Expiration { get; set; }
}
