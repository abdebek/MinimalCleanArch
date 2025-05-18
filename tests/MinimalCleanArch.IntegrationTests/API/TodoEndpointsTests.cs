using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MinimalCleanArch.Sample.API.Models;
using MinimalCleanArch.Sample.Infrastructure.Data;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.TestHost;

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
                // Replace real DB with in-memory for testing
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });
                
                // Ensure services are built before seeding
                var sp = services.BuildServiceProvider();
                
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
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
            Title = "Test Todo",
            Description = "Test Description",
            Priority = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", newTodo);
        var todoResponse = await response.Content.ReadFromJsonAsync<TodoResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        todoResponse.Should().NotBeNull();
        todoResponse!.Title.Should().Be(newTodo.Title);
        todoResponse.Description.Should().Be(newTodo.Description);
        todoResponse.Priority.Should().Be(newTodo.Priority);
    }

    [Fact]
    public async Task CreateTodo_ShouldReturnBadRequest_WhenInvalid()
    {
        // Arrange
        var invalidTodo = new CreateTodoRequest
        {
            // Missing Title, which is required
            Description = "Test Description",
            Priority = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", invalidTodo);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTodo_ShouldReturnNotFound_WhenTodoDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/api/todos/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
