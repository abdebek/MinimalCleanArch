using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MinimalCleanArch.Sample.API.Models;
using MinimalCleanArch.Sample.Infrastructure.Data;
using MinimalCleanArch.Security.Configuration;
using MinimalCleanArch.Security.Encryption;
using FluentAssertions;

namespace MinimalCleanArch.IntegrationTests.API;

public class TodoEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public TodoEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Remove the existing ApplicationDbContext registration
                var contextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ApplicationDbContext));
                
                if (contextDescriptor != null)
                {
                    services.Remove(contextDescriptor);
                }

                // Add test encryption service
                var encryptionOptions = new EncryptionOptions
                {
                    Key = EncryptionOptions.GenerateStrongKey(64), // Use the strong key generator
                    ValidateKeyStrength = false, // Disable validation for tests
                    EnableOperationLogging = false
                };
                
                services.RemoveAll<EncryptionOptions>();
                services.RemoveAll<IEncryptionService>();
                services.AddSingleton(encryptionOptions);
                services.AddSingleton<IEncryptionService, AesEncryptionService>();
                
                // Add test database
                services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
                {
                    options.UseInMemoryDatabase($"IntegrationTestDb_{Guid.NewGuid()}");
                }, ServiceLifetime.Scoped);
                
                // Ensure the test database is created
                var serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.EnsureCreated();
            });
        });
        
        _client = _factory.CreateClient();
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
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

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
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

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
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

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
            await _client.PostAsJsonAsync("/api/todos", todo);
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

        await _client.PostAsJsonAsync("/api/todos", highPriorityTodo);
        await _client.PostAsJsonAsync("/api/todos", lowPriorityTodo);

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
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();

        // Act - Update
        var updateRequest = new UpdateTodoRequest
        {
            Title = createdTodo!.Title,
            Description = "Updated description",
            Priority = 4,
            IsCompleted = true
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/todos/{createdTodo.Id}", updateRequest);
        var updatedTodo = await updateResponse.Content.ReadFromJsonAsync<TodoResponse>();

        // Act - Get
        var getResponse = await _client.GetAsync($"/api/todos/{createdTodo.Id}");
        var retrievedTodo = await getResponse.Content.ReadFromJsonAsync<TodoResponse>();

        // Act - Delete
        var deleteResponse = await _client.DeleteAsync($"/api/todos/{createdTodo.Id}");

        // Assert
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        updatedTodo!.IsCompleted.Should().BeTrue();
        updatedTodo.Priority.Should().Be(4);
        
        retrievedTodo!.IsCompleted.Should().BeTrue();
        retrievedTodo.Description.Should().Be("Updated description");
    }
}