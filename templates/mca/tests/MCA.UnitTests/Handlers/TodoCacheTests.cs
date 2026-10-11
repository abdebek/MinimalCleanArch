#if (UseCaching)
using System.Data;
using System.Linq.Expressions;
using FluentAssertions;
using MCA.Application.Commands;
using MCA.Application.Handlers;
using MCA.Domain.Entities;
using MCA.Domain.Interfaces;
using MinimalCleanArch.Extensions.Caching;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Specifications;
using Xunit;

namespace MCA.UnitTests.Handlers;

public class TodoCacheTests
{
    [Fact]
    public async Task GetTodoById_SecondCall_IsCacheHit()
    {
        var todo = new Todo("cached");
        todo.Id = 42;
        var repository = new CountingTodoRepository(todo);
        var cache = new DictionaryCacheService();
        var handler = new TodoCommandHandler(
            repository,
            new StubUnitOfWork(),
            cache
#if (UseRealtime)
            , MinimalCleanArch.Realtime.NoOpRealtimePublisher.Instance
#endif
            );

        var first = await handler.Handle(new GetTodoByIdQuery(todo.Id), CancellationToken.None);
        var second = await handler.Handle(new GetTodoByIdQuery(todo.Id), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value!.Title.Should().Be("cached");
        second.Value!.Title.Should().Be("cached");
        repository.LoadCalls.Should().Be(1);
    }

#if (!SingleProject)
    [Fact]
    public async Task GetAllTodos_SecondCall_IsCacheHit()
    {
        var todo = new Todo("cached");
        todo.Id = 42;
        var repository = new CountingTodoRepository(todo);
        var cache = new DictionaryCacheService();
        var handler = new TodoCommandHandler(
            repository,
            new StubUnitOfWork(),
            cache
#if (UseRealtime)
            , MinimalCleanArch.Realtime.NoOpRealtimePublisher.Instance
#endif
            );

        var first = await handler.Handle(new GetAllTodosQuery(), CancellationToken.None);
        var second = await handler.Handle(new GetAllTodosQuery(), CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        first.Value!.Items.Should().ContainSingle(x => x.Title == "cached");
        second.Value!.Items.Should().ContainSingle(x => x.Title == "cached");
        repository.LoadAllCalls.Should().Be(1);
    }
#endif

    private sealed class CountingTodoRepository(Todo item) : ITodoRepository
    {
        public int LoadCalls { get; private set; }
        public int LoadAllCalls { get; private set; }

        public Task<IReadOnlyList<Todo>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            LoadAllCalls++;
            return Task.FromResult<IReadOnlyList<Todo>>([item]);
        }

        public Task<IReadOnlyList<Todo>> GetAsync(Expression<Func<Todo, bool>> filter, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Todo>> GetAsync(ISpecification<Todo> specification, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Todo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            LoadCalls++;
            return Task.FromResult(id == item.Id ? item : null);
        }

        public Task<Todo?> GetFirstAsync(Expression<Func<Todo, bool>> filter, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Todo?> GetFirstAsync(ISpecification<Todo> specification, CancellationToken cancellationToken = default)
        {
            LoadCalls++;
            return Task.FromResult<Todo?>(item);
        }

        public Task<int> CountAsync(Expression<Func<Todo, bool>>? filter = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Todo> AddAsync(Todo entity, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IEnumerable<Todo>> AddRangeAsync(IEnumerable<Todo> entities, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Todo> UpdateAsync(Todo entity, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IEnumerable<Todo>> UpdateRangeAsync(IEnumerable<Todo> entities, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(Todo entity, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task DeleteRangeAsync(IEnumerable<Todo> entities, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Todo?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

#if (SingleProject)
        public Task<IReadOnlyList<Todo>> GetByPriorityAsync(int priority, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
#else
        public Task<IReadOnlyList<Todo>> GetIncompleteByPriorityAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Todo>> GetDueBeforeAsync(DateTime date, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
#endif
#if (UseJobs)
        public Task<int> HardDeleteSoftDeletedOlderThanAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
#endif
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public bool HasActiveTransaction => false;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public int SaveChanges() => 0;
        public Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default) => action();
        public Task ExecuteInTransactionAsync(Func<Task> action, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken cancellationToken = default) => action();
        public void Dispose() { }
    }

    private sealed class DictionaryCacheService : ICacheService
    {
        private readonly Dictionary<string, object?> _store = new(StringComparer.Ordinal);

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (_store.TryGetValue(key, out var value) && value is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            return Task.FromResult(default(T?));
        }

        public async Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            CacheEntryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var existing = await GetAsync<T>(key, cancellationToken);
            if (existing is not null || _store.ContainsKey(key))
            {
                return existing;
            }

            var created = await factory(cancellationToken);
            if (created is not null)
            {
                await SetAsync(key, created, options, cancellationToken);
            }

            return created;
        }

        public Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_store.ContainsKey(key));
        public Task RefreshAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
#endif
