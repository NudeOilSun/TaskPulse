namespace TaskPulse;

public class CreateTaskRequest
{
    public int Id { get; set; }
    public string Title { get; set; }
    public DateTime DueDate { get; set; }
}