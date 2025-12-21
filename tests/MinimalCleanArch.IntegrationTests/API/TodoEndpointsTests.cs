using System.Net;
using System.Net.Http.Json;
using MinimalCleanArch.IntegrationTests.Infrastructure;
using MinimalCleanArch.Sample.API.Models;
using FluentAssertions;

namespace MinimalCleanArch.IntegrationTests.API;

public class TodoEndpointsTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TodoEndpointsTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.EnsureDatabaseCreatedAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateTodo_ShouldReturnCreated_WhenValid()
    {
        // Arrange
        var newTodo = new CreateTodoRequest
        {
            Title = "Integration Test Todo",
            Description = "Test Description for Integration",
            Priority = 3,
            DueDate = DateTime.Now.AddDays(7)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", newTodo);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var todoResponse = await response.Content.ReadFromJsonAsync<TodoResponse>();
        todoResponse.Should().NotBeNull();
        todoResponse!.Title.Should().Be(newTodo.Title);
        todoResponse.Description.Should().Be(newTodo.Description);
        todoResponse.Priority.Should().Be(newTodo.Priority);
        todoResponse.Id.Should().BeGreaterThan(0);
        
        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/todos/{todoResponse.Id}");
    }

    [Fact]
    public async Task CreateTodo_ShouldReturnBadRequest_WhenInvalid()
    {
        // Arrange - Missing required title
        var invalidTodo = new CreateTodoRequest
        {
            Title = "", // Invalid - empty title
            Description = "Test Description",
            Priority = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", invalidTodo);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTodo_ShouldReturnBadRequest_WhenInvalidPriority()
    {
        // Arrange
        var invalidTodo = new CreateTodoRequest
        {
            Title = "Valid Title",
            Description = "Valid Description",
            Priority = 10 // Invalid - priority should be 0-5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", invalidTodo);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTodo_ShouldReturnTodo_WhenExists()
    {
        // Arrange - First create a todo
        var createRequest = new CreateTodoRequest
        {
            Title = "Get Test Todo",
            Description = "Description for Get Test",
            Priority = 2
        };

        var createResponse = await _client.PostAsJsonAsync("/api/todos", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        createdTodo.Should().NotBeNull();

        // Act
        var getResponse = await _client.GetAsync($"/api/todos/{createdTodo!.Id}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var todoResponse = await getResponse.Content.ReadFromJsonAsync<TodoResponse>();
        todoResponse.Should().NotBeNull();
        todoResponse!.Id.Should().Be(createdTodo.Id);
        todoResponse.Title.Should().Be(createRequest.Title);
    }

    [Fact]
    public async Task GetTodo_ShouldReturnNotFound_WhenTodoDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/todos/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTodo_ShouldReturnOk_WhenValid()
    {
        // Arrange - Create a todo first
        var createRequest = new CreateTodoRequest
        {
            Title = "Original Title",
            Description = "Original Description",
            Priority = 1
        };

        var createResponse = await _client.PostAsJsonAsync("/api/todos", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        createdTodo.Should().NotBeNull();

        var updateRequest = new UpdateTodoRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Priority = 4,
            IsCompleted = false
        };

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/todos/{createdTodo!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var updatedTodo = await updateResponse.Content.ReadFromJsonAsync<TodoResponse>();
        updatedTodo.Should().NotBeNull();
        updatedTodo!.Title.Should().Be(updateRequest.Title);
        updatedTodo.Description.Should().Be(updateRequest.Description);
        updatedTodo.Priority.Should().Be(updateRequest.Priority);
        updatedTodo.IsCompleted.Should().Be(updateRequest.IsCompleted);
    }

    [Fact]
    public async Task UpdateTodo_ShouldReturnNotFound_WhenTodoDoesNotExist()
    {
        // Arrange
        var updateRequest = new UpdateTodoRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Priority = 2,
            IsCompleted = false
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/todos/99999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTodo_ShouldReturnNoContent_WhenTodoExists()
    {
        // Arrange - Create a todo first
        var createRequest = new CreateTodoRequest
        {
            Title = "Todo to Delete",
            Description = "This will be deleted",
            Priority = 1
        };

        var createResponse = await _client.PostAsJsonAsync("/api/todos", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        createdTodo.Should().NotBeNull();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/todos/{createdTodo!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify the todo is actually deleted (soft delete)
        var getResponse = await _client.GetAsync($"/api/todos/{createdTodo.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTodo_ShouldReturnNotFound_WhenTodoDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync("/api/todos/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTodos_ShouldReturnPaginatedResults()
    {
        // Arrange - Create multiple todos
        var todos = new List<CreateTodoRequest>();
        for (int i = 1; i <= 5; i++)
        {
            todos.Add(new CreateTodoRequest
            {
                Title = $"Todo {i}",
                Description = $"Description {i}",
                Priority = i % 6
            });
        }

        foreach (var todo in todos)
        {
            var createResponse = await _client.PostAsJsonAsync("/api/todos", todo);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        // Act
        var response = await _client.GetAsync("/api/todos?pageSize=3&pageIndex=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("items");
        content.Should().Contain("pagination");
    }

    [Fact]
    public async Task GetTodos_ShouldFilterByPriority()
    {
        // Arrange - Create todos with different priorities
        var highPriorityTodo = new CreateTodoRequest
        {
            Title = "High Priority",
            Description = "Important task",
            Priority = 5
        };

        var lowPriorityTodo = new CreateTodoRequest
        {
            Title = "Low Priority",
            Description = "Not urgent",
            Priority = 1
        };

        var highResponse = await _client.PostAsJsonAsync("/api/todos", highPriorityTodo);
        highResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var lowResponse = await _client.PostAsJsonAsync("/api/todos", lowPriorityTodo);
        lowResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await _client.GetAsync("/api/todos?priority=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("High Priority");
        content.Should().NotContain("Low Priority");
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task CompleteWorkflow_ShouldWork()
    {
        // Arrange & Act - Create
        var createRequest = new CreateTodoRequest
        {
            Title = "Workflow Test Todo",
            Description = "Testing complete workflow",
            Priority = 3,
            DueDate = DateTime.Now.AddDays(5)
        };

        var createResponse = await _client.PostAsJsonAsync("/api/todos", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        createdTodo.Should().NotBeNull();
        createdTodo!.Id.Should().BeGreaterThan(0);

        // Act - Update
        var updateRequest = new UpdateTodoRequest
        {
            Title = createdTodo.Title,
            Description = "Updated description",
            Priority = 4,
            IsCompleted = true
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/todos/{createdTodo.Id}", updateRequest);
        
        // Debug information if update fails
        if (updateResponse.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await updateResponse.Content.ReadAsStringAsync();
            throw new Exception($"Update failed with status {updateResponse.StatusCode}: {errorContent}");
        }
        
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTodo = await updateResponse.Content.ReadFromJsonAsync<TodoResponse>();
        updatedTodo.Should().NotBeNull();

        // Act - Get (verify the update worked)
        var getResponse = await _client.GetAsync($"/api/todos/{createdTodo.Id}");
        
        // Debug information if get fails
        if (getResponse.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await getResponse.Content.ReadAsStringAsync();
            throw new Exception($"Get failed with status {getResponse.StatusCode}: {errorContent}");
        }
        
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedTodo = await getResponse.Content.ReadFromJsonAsync<TodoResponse>();
        retrievedTodo.Should().NotBeNull();

        // Assert update worked correctly
        retrievedTodo!.IsCompleted.Should().BeTrue();
        retrievedTodo.Priority.Should().Be(4);
        retrievedTodo.Description.Should().Be("Updated description");
        retrievedTodo.Title.Should().Be(createdTodo.Title);

        // Act - Delete
        var deleteResponse = await _client.DeleteAsync($"/api/todos/{createdTodo.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion worked (should return 404 now)
        var deletedGetResponse = await _client.GetAsync($"/api/todos/{createdTodo.Id}");
        deletedGetResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTodo_ShouldReturnBadRequest_WhenIdInvalid()
    {
        var response = await _client.GetAsync("/api/todos/0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTodo_ShouldReturnBadRequest_WhenIdInvalid()
    {
        var updateRequest = new UpdateTodoRequest
        {
            Title = "Invalid Id Update",
            Description = "Should fail",
            Priority = 1,
            IsCompleted = false
        };

        var response = await _client.PutAsJsonAsync("/api/todos/0", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteTodo_ShouldReturnBadRequest_WhenIdInvalid()
    {
        var response = await _client.DeleteAsync("/api/todos/0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTodos_ShouldReturnBadRequest_WhenPaginationInvalid()
    {
        var response = await _client.GetAsync("/api/todos?pageSize=0&pageIndex=1");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTodos_ShouldFilterBySearchTerm()
    {
        var token = Guid.NewGuid().ToString("N");
        await CreateTodoAsync(new CreateTodoRequest
        {
            Title = $"Match {token}",
            Description = "Searchable",
            Priority = 1
        });

        await CreateTodoAsync(new CreateTodoRequest
        {
            Title = "Other Todo",
            Description = "No match",
            Priority = 1
        });

        var result = await GetTodosAsync($"?searchTerm={token}");

        result.Items.Should().ContainSingle(todo => todo.Title.Contains(token));
    }

    [Fact]
    public async Task GetTodos_ShouldFilterByCompletionStatus()
    {
        var token = Guid.NewGuid().ToString("N");
        await CreateTodoAsync(new CreateTodoRequest
        {
            Title = $"Pending {token}",
            Description = "Pending",
            Priority = 1
        });

        var completed = await CreateTodoAsync(new CreateTodoRequest
        {
            Title = $"Completed {token}",
            Description = "Completed",
            Priority = 2
        });

        var updateRequest = new UpdateTodoRequest
        {
            Title = completed.Title,
            Description = completed.Description,
            Priority = completed.Priority,
            IsCompleted = true
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/todos/{completed.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await GetTodosAsync("?isCompleted=true");

        result.Items.Should().ContainSingle(todo => todo.Title.Contains($"Completed {token}"));
        result.Items.Should().NotContain(todo => todo.Title.Contains($"Pending {token}"));
    }

    [Fact]
    public async Task GetTodos_ShouldFilterByDueDateRange()
    {
        var token = Guid.NewGuid().ToString("N");
        var nearDue = DateTime.UtcNow.AddDays(1);
        var farDue = DateTime.UtcNow.AddDays(5);

        await CreateTodoAsync(new CreateTodoRequest
        {
            Title = $"Past {token}",
            Description = "Past due",
            Priority = 1,
            DueDate = nearDue
        });

        await CreateTodoAsync(new CreateTodoRequest
        {
            Title = $"Future {token}",
            Description = "Future due",
            Priority = 1,
            DueDate = farDue
        });

        var dueBefore = Uri.EscapeDataString(DateTime.UtcNow.AddDays(2).ToString("O"));
        var result = await GetTodosAsync($"?dueBefore={dueBefore}");

        result.Items.Should().ContainSingle(todo => todo.Title.Contains($"Past {token}"));
        result.Items.Should().NotContain(todo => todo.Title.Contains($"Future {token}"));
    }

    [Fact]
    public async Task EncryptedFields_ShouldBeHandledCorrectly()
    {
        // Arrange
        var sensitiveDescription = "This is sensitive information that should be encrypted";
        var createRequest = new CreateTodoRequest
        {
            Title = "Encryption Test Todo",
            Description = sensitiveDescription,
            Priority = 1
        };

        // Act - Create
        var createResponse = await _client.PostAsJsonAsync("/api/todos", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        createdTodo.Should().NotBeNull();

        // Act - Retrieve
        var getResponse = await _client.GetAsync($"/api/todos/{createdTodo!.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var retrievedTodo = await getResponse.Content.ReadFromJsonAsync<TodoResponse>();

        // Assert
        retrievedTodo.Should().NotBeNull();
        retrievedTodo!.Description.Should().Be(sensitiveDescription);
        retrievedTodo.Title.Should().Be("Encryption Test Todo");
    }

    private async Task<TodoResponse> CreateTodoAsync(CreateTodoRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/todos", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<TodoResponse>();
        created.Should().NotBeNull();
        return created!;
    }

    private async Task<TodoListResponse> GetTodosAsync(string query)
    {
        var response = await _client.GetAsync($"/api/todos{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<TodoListResponse>();
        payload.Should().NotBeNull();
        return payload!;
    }

    private sealed class TodoListResponse
    {
        public List<TodoResponse> Items { get; set; } = new();
        public PaginationInfo Pagination { get; set; } = new();
    }

    private sealed class PaginationInfo
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
