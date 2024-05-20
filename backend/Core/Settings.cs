namespace JwtDemo.Core;

public sealed class Settings
{
    public const string SectionKey = "AppSettings";
    
    public required string Timezone { get; set; }
    public required string ClientOrigin { get; set; }
}

public sealed class AuthSettings
{
    public const string SectionKey = "AuthSettings";
    
    public required string TokenSigningKey { get; set; }
    public required string TokenIssuer { get; set; }
    public required string TokenAudience { get; set; }
    public required int AccessTokenLifetimeMinutes { get; set; }
    public required int RefreshTokenLifetimeMinutes { get; set; }
}
