namespace TaskPulse;

public class TaskItem
{
    public int Id { get; private set; }
    public string Title { get; private set; } = default!;
    public DateTime DueDate { get; private set; }
    public bool IsCompleted { get; private set; }

    private TaskItem() { } // EF

    public TaskItem(string title, DateTime dueDate)
    {
        Title = title;
        DueDate = dueDate;
    }
}