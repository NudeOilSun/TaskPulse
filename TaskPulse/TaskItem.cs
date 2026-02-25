namespace TaskPulse;

public class TaskItem
{
    public int Id { get; private set; }
    public string Title { get; private set; } = default!;
    public DateTime DueDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsDeleted { get; private set; }

    private TaskItem()
    {
    } // EF

    public TaskItem(string title, DateTime dueDate, Boolean isCompleted = false, Boolean isDeleted = false)
    {
        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException("Title cannot be null or empty");
        }

        if (dueDate < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Due date cannot be in the past");
        }
        
        Title = title;
        DueDate = dueDate;
        IsCompleted = isCompleted;
        IsDeleted = isDeleted;
    }

    public void Delete()
    {
        IsDeleted = true;
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }
    
    public void Update(string title, DateTime dueDate)
    {
        if (string.IsNullOrEmpty(title))
        {
            throw new ArgumentException("Title cannot be null or empty");
        }

        if (dueDate < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Due date cannot be in the past");
        }
        
        Title = title;
        DueDate = dueDate;
    }

    public bool IsDueSoon()
    {
        return !this.IsCompleted && !this.IsDeleted &&
            this.DueDate.Date < DateTime.UtcNow.Date.AddDays(4);
    }

    public bool ShouldTriggerReminder()
    {
        if (this.IsDueSoon())
        {
            //do stuff worker here??
            return true;
        }

        return false;
    }
}