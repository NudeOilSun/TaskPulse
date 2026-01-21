using Microsoft.EntityFrameworkCore;
using TaskPulse;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TaskPulseDb>(opt => opt.UseInMemoryDatabase("TaskPulseDb"));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();


app.MapPost("/tasks", async (CreateTaskRequest todo, TaskPulseDb db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});

app.Run();

public partial class Program { }
