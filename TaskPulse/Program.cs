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

app.MapGet("/tasks", async (TaskPulseDb db) => 
    
await db.Tasks
    .AsNoTracking()
    .ToListAsync()
);


app.MapGet("/tasks/{id:int}", async (int id, TaskPulseDb db) =>
{
    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    return task is not null ? Results.Ok(task) : Results.NotFound();
});

app.MapPut("/tasks/{id:int}", async (int id, UpdateTaskRequest updateTaskRequest, TaskPulseDb db) =>
{
    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    if (task == null)
    {
        return Results.NotFound();
    }
    
    task.Title = updateTaskRequest.Title;
    task.DueDate = updateTaskRequest.DueDate;
    
    db.Tasks.Update(task);
    await db.SaveChangesAsync();
    
    return Results.NoContent();
});

app.MapDelete("/tasks/{id:int}", async (int id, TaskPulseDb db) =>
{
    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    if (task == null)
    {
        return Results.NotFound();
    }

    db.Tasks.Remove(task);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public partial class Program { }
