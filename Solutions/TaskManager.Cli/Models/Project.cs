namespace TaskManager.Cli.Models;

public class Project
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Team { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}