using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using TaskPulse;
using Xunit;

namespace TestProjectTests;

public class TaskPulseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public TaskPulseTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
        _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                //Week 3
                // remove existing registration
                // add fresh in-memory db per test
            });
        });
    }

    [Fact]
    public async Task CreateTask_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateTaskRequest("test", DateTime.UtcNow.AddDays(1));

        // Act
        var response = await _client.PostAsJsonAsync("/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateTask_WithoutTitle_ThrowsException(string? title)
    {
        var request = new CreateTaskRequest(title, DateTime.UtcNow.AddDays(1));
        
        // Act
        var response = await _client.PostAsJsonAsync("/tasks", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    } 
    
    [Fact]
    public async Task CreateTask_DueDateInPast_ThrowsException()
    {
        var request = new CreateTaskRequest("title", DateTime.UtcNow.AddDays(-1));
        
        // Act
        var response = await _client.PostAsJsonAsync("/tasks", request);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    } 
    
    [Fact]
    public async Task GetTasks_ValidRequest_ReturnsAllTasks()
    {
        //Arrange
        var testTasks = new List<TaskItem>
        {
            new TaskItem("Title 1", DateTime.UtcNow.AddDays(1)),
            new TaskItem("Title 2", DateTime.UtcNow.AddDays(2)),
        };
    
        // Seed the database with test data
        await SeedDatabaseAsync(testTasks);
    
        // Act
        var response = await _client.GetAsync("/tasks");
    
        //Assert
        response.EnsureSuccessStatusCode();
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskItem>>();
        tasks.Should().HaveCountGreaterThan(1);
        tasks.Should().Contain(t => t.Title == "Title 1");
    }  
    
    [Fact]
    public async Task GetTasks_ValidRequest_ReturnsNoDeletedTasks()
    {
        //Arrange
        var testTasks = new List<TaskItem>
        {
            new TaskItem("Title 1", DateTime.UtcNow.AddDays(1), isDeleted: false),
            new TaskItem("naughty", DateTime.UtcNow.AddDays(2), isDeleted: true),
        };
    
        // Seed the database with test data
        await SeedDatabaseAsync(testTasks);
    
        // Act
        var response = await _client.GetAsync("/tasks");
    
        //Assert
        response.EnsureSuccessStatusCode();
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskItem>>();
        tasks.Should().Contain(t => t.Title == "Title 1");
        tasks.Should().NotContain(t => t.Title == "naughty");
    }

    [Fact]
    public async Task GetTaskById_ValidRequest_ReturnsOk()
    {
        //Arrange
        var testTask = new TaskItem("Title 1", DateTime.UtcNow.AddDays(1));
    
        // Seed the database with test data
        await SeedDatabaseAsync(new List<TaskItem>{testTask});
    
        // Act
        var response = await _client.GetAsync("/tasks/1");
    
        //Assert
        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskItem>();
        task.Should().NotBeNull();
    } 
    
    [Fact]
    public async Task GetTaskById_ResourceMissing_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/tasks/1");
    
        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutTask_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        var testTask = new TaskItem("Title 1", DateTime.UtcNow.AddDays(1));
        await SeedDatabaseAsync(new List<TaskItem>{testTask});

        UpdateTaskRequest updateTaskRequest = new()
        {
            Title = "Update",
            DueDate = DateTime.UtcNow.AddDays(2)
        };
        
        // Act
        var response = await _client.PutAsJsonAsync("/tasks/1", updateTaskRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task PutTask_MissingTitle_ThrowsException(string title)
    {
        // Arrange
        var testTask = new TaskItem("Title 1", DateTime.UtcNow.AddDays(1));
        await SeedDatabaseAsync(new List<TaskItem>{testTask});

        UpdateTaskRequest updateTaskRequest = new()
        {
            Title = title,
            DueDate = DateTime.UtcNow.AddDays(2)
        };
        
        // Act
        var response = await _client.PutAsJsonAsync("/tasks/1", updateTaskRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task PutTask_DueDateInFuture_ThrowsException()
    {
        // Arrange
        var testTask = new TaskItem("Title 1", DateTime.UtcNow.AddDays(1));
        await SeedDatabaseAsync(new List<TaskItem>{testTask});

        UpdateTaskRequest updateTaskRequest = new()
        {
            Title = "title 1",
            DueDate = DateTime.UtcNow.AddDays(-2)
        };
        
        // Act
        var response = await _client.PutAsJsonAsync("/tasks/1", updateTaskRequest);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteTask_UpdateIsCompleted_UpdatesIsCompleted()
    {
        // Arrange
        var testTask = new TaskItem("Title 1", DateTime.UtcNow.AddDays(1), isCompleted: false);
        await SeedDatabaseAsync(new List<TaskItem>{testTask});
    
        // Act
        var response = await _client.PutAsync("/tasks/1/complete", null);
    
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    
        var updatedTask = await GetTaskFromDatabaseAsync(1);
        updatedTask.Should().NotBeNull();
        updatedTask!.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTask_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        var testTask = new TaskItem("Title 1", DateTime.UtcNow.AddDays(1));
        await SeedDatabaseAsync(new List<TaskItem>{testTask});
        
        // Act
        var response = await _client.DeleteAsync("/tasks/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }  
    
    [Fact]
    public async Task DeleteTask_ValidRequest_MarksTaskAsDeleted()
    {
        // Arrange
        var testTask = new TaskItem("Title 1", DateTime.UtcNow.AddDays(1));
        await SeedDatabaseAsync(new List<TaskItem>{testTask});
        
        // Act
        var response = await _client.DeleteAsync("/tasks/1");

        // Assert
        var updatedTask = await GetTaskFromDatabaseAsync(1);
        updatedTask.Should().NotBeNull();
        updatedTask!.IsDeleted.Should().BeTrue();
    }
    
    private async Task SeedDatabaseAsync(List<TaskItem> tasks)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskPulseDb>();
    
        await dbContext.Tasks.AddRangeAsync(tasks);
        await dbContext.SaveChangesAsync();
    }
    
    private async Task<TaskItem?> GetTaskFromDatabaseAsync(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TaskPulseDb>();
    
        return await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    }
}