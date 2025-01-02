using Microsoft.EntityFrameworkCore;
using UnionGen.Types;

namespace JwtDemo.Core.Users;

public interface IUserRepository
{
    public void Add(User newUser);
    public ValueTask<GetUserResult> GetByUsername(string username);
}

public sealed class UserRepository(DbSet<User> users) : IUserRepository
{
    // the tokens should become a JSON column once EF Core supports complex object collections for JSON
    private IQueryable<User> UsersWithRefreshTokens => users.Include(u => u.ActiveRefreshTokens);

    public void Add(User newUser)
    {
        users.Add(newUser);
    }

    public async ValueTask<GetUserResult> GetByUsername(string username)
    {
        var user = await UsersWithRefreshTokens
            .FirstOrDefaultAsync(u => u.Username == username);

        return user is not null ? user : new NotFound();
    }
}
