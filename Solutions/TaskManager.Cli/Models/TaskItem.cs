namespace TaskManager.Cli.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? DueDate { get; set; }
    public string? Assignee { get; set; }
    public string? Project { get; set; }
    public List<string> Tags { get; set; } = [];
}

public enum TaskItemStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}

public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}