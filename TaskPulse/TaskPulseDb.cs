using Microsoft.EntityFrameworkCore;

namespace TaskPulse;

public class TaskPulseDb : DbContext
{
    public TaskPulseDb(DbContextOptions<TaskPulseDb> options)
        : base(options) { }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
}