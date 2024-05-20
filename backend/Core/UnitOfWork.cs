using JwtDemo.Core.Users;
using Microsoft.EntityFrameworkCore.Storage;

namespace JwtDemo.Core;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    public IUserRepository UserRepository { get; }
    public ValueTask BeginTransaction();
    public ValueTask Commit();
    public ValueTask Rollback();
}

public sealed class UnitOfWork(DatabaseContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;
    private int _transactionDepth;

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

    public async ValueTask BeginTransaction()
    {
        if (_transactionDepth == 0)
        {
            _transaction = await context.Database.BeginTransactionAsync();
        }

        _transactionDepth++;
    }

    public async ValueTask Commit()
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No transaction started");
        }

        _transactionDepth--;
        if (_transactionDepth == 0)
        {
            await context.SaveChangesAsync();
            await _transaction.CommitAsync();
            _transaction = null;
        }
    }

    public async ValueTask Rollback()
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No transaction started");
        }

        _transactionDepth--;
        if (_transactionDepth == 0)
        {
            // deliberately not rolling back everything if any sub transaction fails
            // it is expected that an exception will be thrown and the transaction will be rolled back
            // at the caller location as with any other exception
            await _transaction.RollbackAsync();
            _transaction = null;
        }
    }
}
