using System.Linq.Expressions;
using FluentAssertions;
using MinimalCleanArch.Domain.Entities;
using MinimalCleanArch.Repositories;
using MinimalCleanArch.Specifications;

namespace MinimalCleanArch.UnitTests.Repositories;

public class RepositoryDefaultInterfaceTests
{
    [Fact]
    public async Task AnyAsync_UsesDefaultImplementation_ForDirectImplementors()
    {
        IRepository<TestTodo> repository = new InMemoryTodoRepository();

        var result = await repository.AnyAsync(todo => todo.Title == "Second");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SingleOrDefaultAsync_UsesDefaultImplementation_ForDirectImplementors()
    {
        IRepository<TestTodo> repository = new InMemoryTodoRepository();

        var result = await repository.SingleOrDefaultAsync(todo => todo.Title == "First");

        result.Should().NotBeNull();
        result!.Title.Should().Be("First");
    }

    [Fact]
    public async Task CountAsync_WithSpecification_UsesDefaultImplementation_ForDirectImplementors()
    {
        IRepository<TestTodo> repository = new InMemoryTodoRepository();
        var specification = new TodoTitleSpecification("Second");

        var result = await repository.CountAsync(specification);

        result.Should().Be(1);
    }

    private sealed class InMemoryTodoRepository : IRepository<TestTodo>
    {
        private readonly List<TestTodo> _items =
        [
            new TestTodo(1, "First"),
            new TestTodo(2, "Second")
        ];

        public Task<IReadOnlyList<TestTodo>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TestTodo>>(_items);

        public Task<IReadOnlyList<TestTodo>> GetAsync(Expression<Func<TestTodo, bool>> filter, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TestTodo>>(_items.AsQueryable().Where(filter).ToList());

        public Task<IReadOnlyList<TestTodo>> GetAsync(ISpecification<TestTodo> specification, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TestTodo>>(InMemorySpecificationEvaluator.Evaluate(_items, specification).ToList());

        public Task<TestTodo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

        public Task<TestTodo?> GetFirstAsync(Expression<Func<TestTodo, bool>> filter, CancellationToken cancellationToken = default)
            => Task.FromResult(_items.AsQueryable().FirstOrDefault(filter));

        public Task<TestTodo?> GetFirstAsync(ISpecification<TestTodo> specification, CancellationToken cancellationToken = default)
            => Task.FromResult(InMemorySpecificationEvaluator.Evaluate(_items, specification).FirstOrDefault());

        public Task<int> CountAsync(Expression<Func<TestTodo, bool>>? filter = null, CancellationToken cancellationToken = default)
            => Task.FromResult(filter is null ? _items.Count : _items.AsQueryable().Count(filter));

        public Task<TestTodo> AddAsync(TestTodo entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.FromResult(entity);
        }

        public Task<IEnumerable<TestTodo>> AddRangeAsync(IEnumerable<TestTodo> entities, CancellationToken cancellationToken = default)
        {
            var materialized = entities.ToList();
            _items.AddRange(materialized);
            return Task.FromResult<IEnumerable<TestTodo>>(materialized);
        }

        public Task<TestTodo> UpdateAsync(TestTodo entity, CancellationToken cancellationToken = default)
            => Task.FromResult(entity);

        public Task<IEnumerable<TestTodo>> UpdateRangeAsync(IEnumerable<TestTodo> entities, CancellationToken cancellationToken = default)
            => Task.FromResult(entities);

        public Task DeleteAsync(TestTodo entity, CancellationToken cancellationToken = default)
        {
            _items.Remove(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = _items.SingleOrDefault(x => x.Id == id);
            if (entity is not null)
            {
                _items.Remove(entity);
            }

            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(IEnumerable<TestTodo> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities.ToList())
            {
                _items.Remove(entity);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TodoTitleSpecification : BaseSpecification<TestTodo>
    {
        public TodoTitleSpecification(string title)
            : base(todo => todo.Title == title)
        {
        }
    }

    private sealed class TestTodo : IEntity<int>
    {
        public TestTodo(int id, string title)
        {
            Id = id;
            Title = title;
        }

        public int Id { get; set; }

        public string Title { get; }
    }
}
