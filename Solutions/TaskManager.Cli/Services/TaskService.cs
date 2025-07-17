using System.Text.Json;
using TaskManager.Cli.Models;

namespace TaskManager.Cli.Services;

public class TaskService
{
    private readonly string dataFile;
    private List<TaskItem> tasks = [];
    private int nextId;

    public TaskService()
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "taskmanager");
        Directory.CreateDirectory(appData);
        dataFile = Path.Combine(appData, "tasks.json");
        LoadTasks();
    }

    private void LoadTasks()
    {
        if (File.Exists(dataFile))
        {
            string json = File.ReadAllText(dataFile);
            tasks = JsonSerializer.Deserialize<List<TaskItem>>(json) ?? [];
            nextId = tasks.Any() ? tasks.Max(t => t.Id) + 1 : 1;
        }
        else
        {
            tasks = [];
            nextId = 1;
        }
    }

    private void SaveTasks()
    {
        string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dataFile, json);
    }

    public IEnumerable<TaskItem> GetAllTasks() => tasks;

    public TaskItem? GetTask(int id) => tasks.FirstOrDefault(t => t.Id == id);

    public TaskItem AddTask(TaskItem task)
    {
        task.Id = nextId++;
        task.CreatedAt = DateTime.Now;
        tasks.Add(task);
        SaveTasks();

        return task;
    }

    public bool UpdateTask(TaskItem task)
    {
        int index = tasks.FindIndex(t => t.Id == task.Id);
        if (index >= 0)
        {
            tasks[index] = task;
            SaveTasks();
            return true;
        }
        return false;
    }

    public bool DeleteTask(int id)
    {
        bool removed = tasks.RemoveAll(t => t.Id == id) > 0;
        if (removed) SaveTasks();

        return removed;
    }
}