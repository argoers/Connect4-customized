using System.Text.Json;
using BLL;

namespace DAL;

public class GameRepositoryJson : IGameRepository<GameBrain>
{
    public List<(string id, string description, string date, bool isFinished)> List()
    {
        var dir = FilesystemHelpers.GetGameDirectory();
        var res = new List<(string id, string description, string date, bool isFinished)>();

        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Tallinn");
        foreach (var fullFileName in Directory.EnumerateFiles(dir))
        {
            var fileName = Path.GetFileName(fullFileName);
            if (!fileName.EndsWith(".json")) continue;

            var jsonTxt = File.ReadAllText(fullFileName);
            var game = JsonSerializer.Deserialize<GameBrain>(jsonTxt);

            if (game != null)
            {
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(game.CreatedOn, tz);
                res.Add((
                    game.Id.ToString(),
                    game.GameConfiguration?.Name ?? "Unknown",
                    localTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    game.IsGameFinished
                ));
            }
        }
        
        return res.OrderByDescending(x => x.date).ToList();
    }
    
    public async Task<List<(string id, string description, string date, bool isFinished)>> ListAsync()
    {
        var dir = FilesystemHelpers.GetGameDirectory();
        var res = new List<(string id, string description, string date, bool isFinished)>();

        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Tallinn");
        foreach (var fullFileName in Directory.EnumerateFiles(dir))
        {
            var fileName = Path.GetFileName(fullFileName);
            if (!fileName.EndsWith(".json")) continue;

            var jsonTxt = await File.ReadAllTextAsync(fullFileName);
            var game = JsonSerializer.Deserialize<GameBrain>(jsonTxt);

            if (game != null)
            {
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(game.CreatedOn, tz);
                res.Add((
                    game.Id.ToString(),
                    game.GameConfiguration.Name,
                    localTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    game.IsGameFinished
                ));
            }
        }

        return res.OrderByDescending(x => x.date).ToList();
    }

    public string Save(GameBrain data)
    {
        // Sync 2D array into GameBoardJson before writing
        data.SyncToJson();

        var jsonStr = JsonSerializer.Serialize(data);
        var newFileName = $"{data.Id}.json";
        var newFullPath = FilesystemHelpers.GetGameDirectory() + Path.DirectorySeparatorChar + newFileName;
        File.WriteAllText(newFullPath, jsonStr);

        return data.Id.ToString();
    }
    
    public async Task<string> SaveAsync(GameBrain data)
    {
        // Sync 2D array into GameBoardJson before writing
        data.SyncToJson();

        var jsonStr = JsonSerializer.Serialize(data);
        var newFileName = $"{data.Id}.json";
        var newFullPath = FilesystemHelpers.GetGameDirectory() + Path.DirectorySeparatorChar + newFileName;

        await File.WriteAllTextAsync(newFullPath, jsonStr);

        return data.Id.ToString();
    }

    public GameBrain Load(string id)
    {
        var dir = FilesystemHelpers.GetGameDirectory();
        var fullFileName = Path.Combine(dir, $"{id}.json");

        if (!File.Exists(fullFileName))
            throw new FileNotFoundException($"Game save file '{id}.json' not found.");

        var jsonTxt = File.ReadAllText(fullFileName);
        var game = JsonSerializer.Deserialize<GameBrain>(jsonTxt);

        if (game == null)
            throw new NullReferenceException($"Json deserialization returned null for file: {fullFileName}");

        game.SyncFromJson();
        return game;
    }
    
    public async Task<GameBrain> LoadAsync(string id)
    {
        var dir = FilesystemHelpers.GetGameDirectory();
        var fullFileName = Path.Combine(dir, $"{id}.json");

        if (!File.Exists(fullFileName))
            throw new FileNotFoundException($"Game save file '{id}.json' not found.");

        var jsonTxt = await File.ReadAllTextAsync(fullFileName);
        var game = JsonSerializer.Deserialize<GameBrain>(jsonTxt);

        if (game == null)
            throw new NullReferenceException($"Json deserialization returned null for file: {fullFileName}");

        game.SyncFromJson();
        return game;
    }

    public void Delete(string id)
    {
        var dir = FilesystemHelpers.GetGameDirectory();
        var fullFileName = Path.Combine(dir, $"{id}.json");
        if (File.Exists(fullFileName))
        {
            File.Delete(fullFileName);
        }
    }
    
    public Task DeleteAsync(string id)
    {
        Delete(id);
        return Task.CompletedTask;
    }
}
