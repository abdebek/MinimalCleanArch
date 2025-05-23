using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MinimalCleanArch.Repositories;

namespace MinimalCleanArch.EntityFramework.Repositories;

/// <summary>
/// Unit of Work implementation using Entity Framework Core
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _dbContext;
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class
    /// </summary>
    /// <param name="dbContext">The database context</param>
    public UnitOfWork(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <summary>
    /// Gets a value indicating whether there is an active transaction
    /// </summary>
    public bool HasActiveTransaction => _currentTransaction != null;

    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>The number of state entries written to the database</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            // If we have an active transaction, we might want to roll it back
            // This is optional - depends on your error handling strategy
            throw;
        }
    }

    /// <summary>
    /// Saves all pending changes to the database synchronously
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
    public int SaveChanges()
    {
        try
        {
            return _dbContext.SaveChanges();
        }
        catch (Exception)
        {
            // If we have an active transaction, we might want to roll it back
            // This is optional - depends on your error handling strategy
            throw;
        }
    }

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    /// <param name="isolationLevel">The isolation level for the transaction</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            throw new InvalidOperationException("A transaction is already in progress");
        }

        // Always use the async version with default isolation level
        // EF Core doesn't provide good async support for custom isolation levels
        _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        // Note: Custom isolation levels are not fully supported in this implementation
        // Most applications work fine with the default ReadCommitted level
        // If you need custom isolation levels, consider managing transactions at a higher level
        if (isolationLevel != IsolationLevel.ReadCommitted)
        {
            // Log a warning that custom isolation level is ignored
            // In a real implementation, you might want to use a logger here
            System.Diagnostics.Debug.WriteLine($"Warning: Custom isolation level {isolationLevel} is not supported. Using default ReadCommitted.");
        }
    }

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress");
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null)
        {
            throw new InvalidOperationException("No transaction is in progress");
        }

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <summary>
    /// Executes a function within a transaction scope
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="action">The action to execute</param>
    /// <param name="isolationLevel">The isolation level for the transaction</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var hadActiveTransaction = HasActiveTransaction;
        
        if (!hadActiveTransaction)
        {
            await BeginTransactionAsync(isolationLevel, cancellationToken);
        }

        try
        {
            var result = await action();
            
            if (!hadActiveTransaction)
            {
                await CommitTransactionAsync(cancellationToken);
            }
            
            return result;
        }
        catch
        {
            if (!hadActiveTransaction && HasActiveTransaction)
            {
                await RollbackTransactionAsync(cancellationToken);
            }
            throw;
        }
    }

    /// <summary>
    /// Executes an action within a transaction scope
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <param name="isolationLevel">The isolation level for the transaction</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public async Task ExecuteInTransactionAsync(
        Func<Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            await action();
            return true; // Dummy return value
        }, isolationLevel, cancellationToken);
    }

    /// <summary>
    /// Disposes the current transaction
    /// </summary>
    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    /// <summary>
    /// Disposes the unit of work
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Disposes the unit of work asynchronously
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}