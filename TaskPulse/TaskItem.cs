namespace TaskPulse;

public class TaskItem
{
    public int Id { get;set; }
    public string Title { get;set; } = default!;
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }

    private TaskItem() { } // EF

    public TaskItem(string title, DateTime dueDate)
    {
        Title = title;
        DueDate = dueDate;
    }
}