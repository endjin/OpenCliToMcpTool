using System.Text.Json;
using TaskManager.Cli.Models;

namespace TaskManager.Cli.Services;

public class ProjectService
{
    private readonly string dataFile;
    private List<Project> projects = [];

    public ProjectService()
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "taskmanager");
        Directory.CreateDirectory(appData);
        dataFile = Path.Combine(appData, "projects.json");
        LoadProjects();
    }

    private void LoadProjects()
    {
        if (File.Exists(dataFile))
        {
            string json = File.ReadAllText(dataFile);
            projects = JsonSerializer.Deserialize<List<Project>>(json) ?? [];
        }
        else
        {
            projects = [];
        }
    }

    private void SaveProjects()
    {
        string json = JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dataFile, json);
    }

    public IEnumerable<Project> GetAllProjects() => projects;

    public Project? GetProject(string name) => projects.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public Project AddProject(Project project)
    {
        project.CreatedAt = DateTime.Now;
        projects.Add(project);
        SaveProjects();
        return project;
    }

    public bool ArchiveProject(string name)
    {
        Project? project = GetProject(name);
        if (project != null)
        {
            project.IsActive = false;
            SaveProjects();
            return true;
        }
        return false;
    }
}