using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskPulse;

var builder = WebApplication.CreateBuilder(args);

var connection = new SqliteConnection("DataSource=:memory:");
connection.Open();

builder.Services.AddDbContext<TaskPulseDb>(options =>
{
    options.UseSqlite(connection);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TaskPulseDb>();
    db.Database.EnsureCreated();
}


app.MapPost("/tasks", async (
    CreateTaskRequest request,
    TaskPulseDb db,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest("Title is required");

    var task = new TaskItem(request.Title, request.DueDate);

    db.Tasks.Add(task);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/tasks/{task.Id}", task);
});

app.Run();

public partial class Program { }
