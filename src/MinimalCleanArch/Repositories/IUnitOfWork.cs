using System.Data;

namespace MinimalCleanArch.Repositories;

/// <summary>
/// Unit of Work pattern interface for managing transactions and change persistence
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all pending changes to the database
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>The number of state entries written to the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes to the database synchronously
    /// </summary>
    /// <returns>The number of state entries written to the database</returns>
    int SaveChanges();

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    /// <param name="isolationLevel">The isolation level for the transaction</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether there is an active transaction
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Executes a function within a transaction scope
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="action">The action to execute</param>
    /// <param name="isolationLevel">The isolation level for the transaction</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an action within a transaction scope
    /// </summary>
    /// <param name="action">The action to execute</param>
    /// <param name="isolationLevel">The isolation level for the transaction</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    Task ExecuteInTransactionAsync(
        Func<Task> action,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}
