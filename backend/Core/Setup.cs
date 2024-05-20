using System.Text;
using JwtDemo.Core.Auth;
using JwtDemo.Core.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NodaTime;
using NodaTime.Extensions;

namespace JwtDemo.Core;

public static class Setup
{
    public const string CorsPolicyName = "CorsPolicy";
    
    public static void LoadConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Settings>(configuration.GetSection(Settings.SectionKey));
        services.Configure<AuthSettings>(configuration.GetSection(AuthSettings.SectionKey));
    }
    
    public static void RegisterServices(this IServiceCollection services)
    {
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<ITokenProvider, TokenProvider>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserService, UserService>();
        
        services.AddSingleton<IClock>(SystemClock.Instance);
    }

    public static void ConfigureAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(AuthSettings.SectionKey).Get<AuthSettings>();
        var tokenSigningKey = settings?.TokenSigningKey;
        var tokenIssuer = settings?.TokenIssuer;
        var tokenAudience = settings?.TokenAudience;
        
        if (string.IsNullOrWhiteSpace(tokenSigningKey) 
            || string.IsNullOrWhiteSpace(tokenIssuer) 
            || string.IsNullOrWhiteSpace(tokenAudience))
        {
            throw new InvalidOperationException("JWT settings are missing");
        }
        
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSigningKey));
        var clockSkew = Duration.FromSeconds(1);
        var clock = SystemClock.Instance.InUtc();

        services.AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(o =>
                {
                    o.SaveToken = true;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,

                        IssuerSigningKey = signingKey,
                        ValidIssuer = tokenIssuer,
                        ValidAudience = tokenAudience,
                        ClockSkew = clockSkew.ToTimeSpan(),
                        LifetimeValidator = (nb, ex, _, _) =>
                        {
                            var nbInstant = Instant.FromDateTimeUtc(nb ?? DateTime.MaxValue);
                            var exInstant = Instant.FromDateTimeUtc(ex ?? DateTime.MinValue);

                            var now = clock.GetCurrentInstant();
                            var notBefore = nbInstant.Plus(clockSkew * -1);
                            var expires = exInstant.Plus(clockSkew);

                            return now >= notBefore && now <= expires;
                        }
                    };
                });
    }

    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "JWT Demo",
                Version = "v1"
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    []
                }
            });
        });
    }
    
    public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(Settings.SectionKey).Get<Settings>();
        var corsOrigin = settings?.ClientOrigin;
        
        if (string.IsNullOrWhiteSpace(corsOrigin))
        {
            throw new InvalidOperationException("Client origin is missing");
        }
        
        services.AddCors(o =>
        {
            o.AddPolicy(CorsPolicyName, b =>
            {
                b.WithOrigins(corsOrigin)
                 .AllowAnyMethod()
                 .AllowAnyHeader()
                 .AllowCredentials();
            });
        });
    }

    public static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrWhiteSpace(connString))
        {
            throw new InvalidOperationException("Database path is missing");
        }
        
        services.AddDbContext<DatabaseContext>(optionsBuilder =>
        {
            optionsBuilder.UseSqlite(connString, dbOptions =>
            {
                dbOptions.UseNodaTime();
            });
        });
    }
    
    public static async ValueTask UpdateDatabase(this IApplicationBuilder app)
    {
        using var serviceScope =
            app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        await using var context = serviceScope.ServiceProvider.GetService<DatabaseContext>();

        await context!.Database.MigrateAsync();
    }
}
