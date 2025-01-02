using JwtDemo.Core.Products;
using JwtDemo.Core.Users;
using Microsoft.EntityFrameworkCore.Storage;

namespace JwtDemo.Core;

public interface ITransactionProvider
{
    public ValueTask BeginTransactionAsync();
    public ValueTask CommitAsync();
    public ValueTask RollbackAsync();
}

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    public IUserRepository UserRepository { get; }
    public IProductRepository ProductRepository { get; }
}

public sealed class UnitOfWork(DatabaseContext context) : IUnitOfWork, ITransactionProvider
{
    private IDbContextTransaction? _transaction;

    public async ValueTask DisposeAsync()
    {
        if (_transaction is null)
        {
            return;
        }

        // Transaction was neither committed nor rolled back, rolling back now
        await _transaction.RollbackAsync();
    }

    public void Dispose()
    {
        if (_transaction is null)
        {
            return;
        }

        _transaction.Rollback();
        _transaction.Dispose();
    }

    public IUserRepository UserRepository => new UserRepository(context.Users);
    public IProductRepository ProductRepository => new ProductRepository(context.Products);

    public async ValueTask BeginTransactionAsync()
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("Transaction already started");
        }

        _transaction = await context.Database.BeginTransactionAsync();
    }

    public async ValueTask CommitAsync()
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No transaction started");
        }

        await context.SaveChangesAsync();
        await _transaction.CommitAsync();
        _transaction = null;
    }

    public async ValueTask RollbackAsync()
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No transaction started");
        }

        await _transaction.RollbackAsync();
        _transaction = null;
    }
}
