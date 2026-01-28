namespace TaskPulse;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = default!;
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }

    private TaskItem()
    {
    } // EF

    public TaskItem(string title, DateTime dueDate, Boolean isCompleted = false, Boolean isDeleted = false)
    {
        Title = title;
        DueDate = dueDate;
        IsCompleted = isCompleted;
        IsDeleted = isDeleted;
    }

    public void Delete()
    {
        IsDeleted = true;
    }

    public void UpdateCompleted()
    {
        IsCompleted = true;
    }
    
    public void Update(string title, DateTime dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required");
        
        Title = title;
        DueDate = dueDate;
    }
}