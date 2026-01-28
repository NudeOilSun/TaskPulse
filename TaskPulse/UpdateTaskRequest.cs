namespace TaskPulse;

public class UpdateTaskRequest
{
    public String Title { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
}