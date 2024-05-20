using JwtDemo.Core.Auth;
using JwtDemo.Core.Users;
using Microsoft.EntityFrameworkCore;

namespace JwtDemo.Core;

public sealed class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public required DbSet<User> Users { get; set; }
    public required DbSet<ActiveRefreshToken> ActiveRefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        ConfigureUser(modelBuilder);
        ConfigureActiveRefreshToken(modelBuilder);
    }

    private static void ConfigureActiveRefreshToken(ModelBuilder modelBuilder)
    {
        var activeRefreshToken = modelBuilder.Entity<ActiveRefreshToken>();
        activeRefreshToken.HasKey(a => a.Id);
        activeRefreshToken.Property(a => a.Id).ValueGeneratedOnAdd();
        activeRefreshToken.HasIndex(a => a.UserId);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<User>();
        user.HasKey(u => u.Id);
        user.Property(u => u.Id).ValueGeneratedOnAdd();
        user.Property(u => u.Username).HasMaxLength(60);
        user.HasIndex(u => u.Username).IsUnique();
        user.HasMany(u => u.ActiveRefreshTokens)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
