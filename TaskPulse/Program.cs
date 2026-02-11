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

        if (exception is ArgumentException)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync(exception.Message);
        }
    });
});

app.MapPost("/tasks", async (
    CreateTaskRequest request,
    TaskPulseDb db,
    CancellationToken ct) =>
{
    var task = new TaskItem(request.Title, request.DueDate);

    db.Tasks.Add(task);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/tasks/{task.Id}", task);
});

app.MapGet("/tasks", async (TaskPulseDb db) =>
    {
        var tasks = await db.Tasks
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .ToListAsync();
        
        return Results.Ok(tasks);
    }
);


app.MapGet("/tasks/{id:int}", async (int id, TaskPulseDb db) =>
{
    var task = await db.Tasks
        .AsNoTracking()
        .FirstOrDefaultAsync(t => t.Id == id);
    
    return task is not null ? Results.Ok(task) : Results.NotFound();
});

app.MapPut("/tasks/{id:int}", async (int id, UpdateTaskRequest updateTaskRequest, TaskPulseDb db) =>
{
    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    if (task == null)
    {
        return Results.NotFound();
    }
    
    task.Update(updateTaskRequest.Title, updateTaskRequest.DueDate);
    
    await db.SaveChangesAsync();
    
    return Results.NoContent();
});

app.MapPut("/tasks/{id:int}/complete", async (int id, TaskPulseDb db) =>
{
    var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
    if (task == null)
    {
        return Results.NotFound();
    }

    task.MarkCompleted();

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

    task.Delete();

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public partial class Program { }
