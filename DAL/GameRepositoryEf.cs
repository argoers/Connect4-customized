using BLL;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class GameRepositoryEf : IGameRepository<GameBrain>
{
    private readonly AppDbContext _dbContext;

    public GameRepositoryEf(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<(string id, string description, string date, bool isFinished)> List()
    {
        var games = _dbContext.GameBrains
            .Include(g => g.GameConfiguration)
            .OrderByDescending(g => g.CreatedOn)
            .ToList();

        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Tallinn");
        var res = new List<(string id, string description, string date,bool isFinished)>();
        foreach (var g in games)
        {
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(g.CreatedOn, tz);
            res.Add(
                (
                    g.Id.ToString(), 
                    g.GameConfiguration.Name,
                    g.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss"),
                    g.IsGameFinished
                )
            );
        }

        return res;
    }

    public async Task<List<(string id, string description, string date, bool isFinished)>> ListAsync()
    {
        var games = await _dbContext.GameBrains
            .Include(g => g.GameConfiguration)
            .OrderByDescending(g => g.CreatedOn)
            .ToListAsync();

        var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Tallinn");
        var res = new List<(string id, string description, string date,bool isFinished)>();
        foreach (var g in games)
        {
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(g.CreatedOn, tz);
            res.Add(
                (
                    g.Id.ToString(), 
                    g.GameConfiguration.Name,
                    localTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    g.IsGameFinished
                )
            );
        }

        return res;
    }

    public string Save(GameBrain data)
    {
        data.SyncToJson();
        var existing = _dbContext.GameBrains.FirstOrDefault(c => c.Id == data.Id);
        if (existing != null)
        {
            existing.Player1Name = data.Player1Name;
            existing.Player2Name = data.Player2Name;
            existing.NextMoveByBlue = data.NextMoveByBlue;
            existing.GameBoardJson = data.GameBoardJson;
            existing.GameConfigurationId = data.GameConfigurationId;

            _dbContext.Update(existing);
            _dbContext.SaveChanges();
            return existing.Id.ToString();
        }
        

        _dbContext.GameBrains.Add(data);
        _dbContext.SaveChanges();
        return data.Id.ToString();    
    }
    
    public async Task<string> SaveAsync(GameBrain data)
    {
        data.SyncToJson();
        var existing = await _dbContext.GameBrains.FirstOrDefaultAsync(c => c.Id == data.Id);
        if (existing != null)
        {
            existing.Player1Name = data.Player1Name;
            existing.Player2Name = data.Player2Name;
            existing.NextMoveByBlue = data.NextMoveByBlue;
            existing.GameBoardJson = data.GameBoardJson;
            existing.GameConfigurationId = data.GameConfigurationId;

            _dbContext.Update(existing);
            await _dbContext.SaveChangesAsync();
            return existing.Id.ToString();
        }

        await _dbContext.GameBrains.AddAsync(data);
        await _dbContext.SaveChangesAsync();
        return data.Id.ToString();
    }

    public GameBrain Load(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(id));
        
        var config = _dbContext.GameBrains
            .Include(g => g.GameConfiguration)
            .FirstOrDefault(c => c.Id == guid);
        if (config == null)
            throw new KeyNotFoundException($"Game configuration with ID '{id}' not found.");
        
        config.SyncFromJson();
        return config;
    }
    
    public async Task<GameBrain> LoadAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(id));

        var config = await _dbContext.GameBrains
            .Include(g => g.GameConfiguration)
            .FirstOrDefaultAsync(c => c.Id == guid);

        if (config == null)
            throw new KeyNotFoundException($"Game configuration with ID '{id}' not found.");

        config.SyncFromJson();
        return config;
    }

    public void Delete(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(id));
        
        var config = _dbContext.GameBrains.FirstOrDefault(c => c.Id == guid);
        if (config != null)
        {
            _dbContext.GameBrains.Remove(config);
            _dbContext.SaveChanges();
        }
    }
    
    public async Task DeleteAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(id));

        var config = await _dbContext.GameBrains.FirstOrDefaultAsync(c => c.Id == guid);
        if (config != null)
        {
            _dbContext.GameBrains.Remove(config);
            await _dbContext.SaveChangesAsync();
        }
    }
}