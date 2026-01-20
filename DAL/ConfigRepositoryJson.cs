using System.Text.Json;
using BLL;

namespace DAL;

public class ConfigRepositoryJson : IConfigRepository<GameConfiguration>
{
    public List<(string id, string description)> List()
    {
        var dir = FilesystemHelpers.GetConfigDirectory();
        var res = new List<(string id, string description)>();
        foreach (var fullFileName in Directory.EnumerateFiles(dir))
        {
            var fileName = Path.GetFileName(fullFileName);
            if (!fileName.EndsWith(".json")) continue;
            var jsonTxt = File.ReadAllText(fullFileName);
            var conf = JsonSerializer.Deserialize<GameConfiguration>(jsonTxt);
            res.Add((conf!.Id.ToString(),Path.GetFileNameWithoutExtension(fileName)));
        }
        return res;
    }

    public async Task<List<(string id, string description)>> ListAsync()
    {
        var dir = FilesystemHelpers.GetConfigDirectory();
        var res = new List<(string id, string description)>();
        foreach (var fullFileName in Directory.EnumerateFiles(dir))
        {
            var fileName = Path.GetFileName(fullFileName);
            if (!fileName.EndsWith(".json")) continue;
            var jsonTxt = await File.ReadAllTextAsync(fullFileName);
            var conf = JsonSerializer.Deserialize<GameConfiguration>(jsonTxt);

            res.Add((conf!.Id.ToString(), Path.GetFileNameWithoutExtension(fullFileName)));
        }

        return res;
    }

    public string Save(GameConfiguration data, string? id =  null)
    {
        var jsonStr = JsonSerializer.Serialize(data);
        
        var newFileName = $"{data.Name}" + ".json";
        var newFullPath = FilesystemHelpers.GetConfigDirectory() + Path.DirectorySeparatorChar + newFileName;
        
        var existingFiles = List();
        var existing = existingFiles.FirstOrDefault(x => x.id == id);
        
        if (!string.IsNullOrEmpty(existing.description))
        {
            var oldFullPath = FilesystemHelpers.GetConfigDirectory() + Path.DirectorySeparatorChar + existing.description + ".json";
            if (File.Exists(oldFullPath) && !oldFullPath.Equals(newFullPath))
            {
                File.Move(oldFullPath, newFullPath, overwrite: true);
            }
        }
        File.WriteAllText(newFullPath, jsonStr);
        
        return newFileName;
    }
    
    public async Task<string> SaveAsync(GameConfiguration data, string? id = null)
    {
        var jsonStr = JsonSerializer.Serialize(data);

        var newFileName = $"{data.Name}.json";
        var newFullPath = Path.Combine(FilesystemHelpers.GetConfigDirectory(), newFileName);

        var existingFiles = await ListAsync();
        var existing = existingFiles.FirstOrDefault(x => x.id == id);

        if (!string.IsNullOrEmpty(existing.description))
        {
            var oldFullPath = FilesystemHelpers.GetConfigDirectory() + Path.DirectorySeparatorChar + existing.description + ".json";
            if (File.Exists(oldFullPath) && oldFullPath != newFullPath)
            {
                File.Move(oldFullPath, newFullPath, overwrite: true);
            }
        }

        await File.WriteAllTextAsync(newFullPath, jsonStr);
        return newFileName;
    }

    public GameConfiguration Load(string id)
    {
        var confDescription = List().Find(item => item.id == id).description;
        var jsonFileName = FilesystemHelpers.GetConfigDirectory() + Path.DirectorySeparatorChar + confDescription + ".json";
        var jsonTxt = File.ReadAllText(jsonFileName);
        var conf = JsonSerializer.Deserialize<GameConfiguration>(jsonTxt);
        
        return conf ?? throw new NullReferenceException($"Json deserialization returned null. Data: {jsonTxt}");
    }
    
    public async Task<GameConfiguration> LoadAsync(string id)
    {
        var confDescription = (await ListAsync()).Find(item => item.id == id).description;
        var jsonFileName = FilesystemHelpers.GetConfigDirectory() + Path.DirectorySeparatorChar + confDescription + ".json";

        var jsonTxt = await File.ReadAllTextAsync(jsonFileName);
        var conf = JsonSerializer.Deserialize<GameConfiguration>(jsonTxt);
        
        return conf ?? throw new NullReferenceException($"Json deserialization returned null. Data: {jsonTxt}");
    }

    public void Delete(string id)
    {
        var confDescription = List().Find(item => item.id == id).description;
        var jsonFileName = FilesystemHelpers.GetConfigDirectory() + Path.DirectorySeparatorChar + confDescription + ".json";
        if (File.Exists(jsonFileName))
        {
            File.Delete(jsonFileName);    
        }
    }

    public async Task DeleteAsync(string id)
    {
        var confDescription = (await ListAsync()).Find(item => item.id == id).description;
        var jsonFileName = FilesystemHelpers.GetConfigDirectory() + Path.DirectorySeparatorChar + confDescription + ".json";
        if (File.Exists(jsonFileName))
        {
            File.Delete(jsonFileName);
        }
    }
}