using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskPulse;
using Xunit;

namespace TestProjectTests;

public class TaskPulseTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TaskPulseTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTask_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Title = "test",
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/tasks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}