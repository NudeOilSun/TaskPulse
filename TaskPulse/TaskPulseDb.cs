using Microsoft.EntityFrameworkCore;

namespace TaskPulse;

public class TaskPulseDb : DbContext
{
    public TaskPulseDb(DbContextOptions<TaskPulseDb> options)
        : base(options) { }

    public DbSet<CreateTaskRequest> Todos => Set<CreateTaskRequest>();
}