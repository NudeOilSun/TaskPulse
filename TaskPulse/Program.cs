using Microsoft.AspNetCore.Diagnostics;
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

app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is ArgumentException ex)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var problem = new
            {
                title = "Validation error",
                status = 400,
                detail = ex.Message
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    });
});

app.MapPost("/tasks", async (
    CreateTaskRequest request,
    TaskPulseDb db,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    logger.LogInformation("Creating task with title {Title}", request.Title);

    var task = new TaskItem(request.Title, request.DueDate);

    db.Tasks.Add(task);
    await db.SaveChangesAsync(ct);
    
    logger.LogInformation("Task {TaskId} created", task.Id);
    
    return Results.Created($"/tasks/{task.Id}", task);
});

app.MapGet("/tasks", async (TaskPulseDb db, ILogger<Program> logger) =>
    {
        logger.LogInformation("GET tasks called");

        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .ToListAsync();
        
        logger.LogInformation($"Successfully retrieved {tasks.Count} tasks from database");

        return Results.Ok(tasks);
    }
);


app.MapGet("/tasks/{id:int}", async (int id, TaskPulseDb db, ILogger<Program> logger) =>
{
    logger.LogInformation($"Get Task with ID called with ID: {id}");

    var task = await db.Tasks
        .AsNoTracking()
        .FirstOrDefaultAsync(t => t.Id == id);
    
    return task is not null ? Results.Ok(task) : Results.NotFound();
});

app.MapGet("/tasks/due-soon", async (TaskPulseDb db, ILogger<Program> logger) =>
{
    logger.LogInformation("Get tasks due soon called");

    var tasks = await db.Tasks
        .AsNoTracking()
        .Where(t => !t.IsCompleted && !t.IsDeleted)
        .ToListAsync();
    
    logger.LogInformation($"Obtained {tasks.Count} tasks due soon");

    var result = tasks.Where(t => t.IsDueSoon()).ToList();

    return Results.Ok(result);
});

app.MapPut("/tasks/{id:int}", async (int id, UpdateTaskRequest updateTaskRequest, TaskPulseDb db,  ILogger<Program> logger) =>
{
    logger.LogInformation("PUT Tasks with ID called with ID: {id}, request: {updateTaskRequest}", id, updateTaskRequest);

    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    if (task == null)
    {
        logger.LogInformation("Unable to locate task with ID {id}", id);
        return Results.NotFound();
    }
    
    task.Update(updateTaskRequest.Title, updateTaskRequest.DueDate);
    logger.LogInformation("Updated Task with ID {id}", id);

    await db.SaveChangesAsync();
    
    return Results.NoContent();
});

app.MapPut("/tasks/{id:int}/complete", async (int id, TaskPulseDb db, ILogger<Program> logger) =>
{
    logger.LogInformation("PUT Tasks complete called with ID: {id}", id);

    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    if (task == null)
    {
        logger.LogInformation("Unable to locate task with ID {id}", id);
        return Results.NotFound();
    }

    task.MarkCompleted();

    db.Tasks.Update(task);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/tasks/{id:int}", async (int id, TaskPulseDb db, ILogger<Program> logger) =>
{
    logger.LogInformation("DELETE Tasks complete called with ID: {id}", id);

    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    if (task == null)
    {
        logger.LogInformation("Unable to locate task with ID {id}", id);
        return Results.NotFound();
    }

    task.Delete();

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public partial class Program { }
