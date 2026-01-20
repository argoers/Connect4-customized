using BLL;
using Microsoft.EntityFrameworkCore;

namespace DAL;

public class ConfigRepositoryEf : IConfigRepository<GameConfiguration>
{
    private readonly AppDbContext _dbContext;

    public ConfigRepositoryEf(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public List<(string id, string description)> List()
    {
        var res = new List<(string id, string description)>();
        foreach (var dbConf in _dbContext.GameConfigurations)
        {
            res.Add(
                (
                    dbConf.Id.ToString(),
                    dbConf.Name
                )
            );
        }

        return res;
    }
    
    public async Task<List<(string id, string description)>> ListAsync()
    {
        var res = new List<(string id, string description)>();
        foreach (var dbConf in await _dbContext.GameConfigurations.ToListAsync())
        {
            res.Add(
                (
                    dbConf.Id.ToString(),
                    dbConf.Name
                )
            );
        }

        return res;
    }

    public async Task<List<(string id, string description, int width, int height, bool isCylindrical)>> ListDetailedAsync()
    {
        var res = new List<(string id, string description, int width, int height, bool isCylindrical)>();
        foreach (var dbConf in await _dbContext.GameConfigurations.ToListAsync())
        {
            res.Add(
                (
                    dbConf.Id.ToString(),
                    dbConf.Name,
                    dbConf.BoardWidth,
                    dbConf.BoardHeight,
                    dbConf.IsBoardCylindrical
                )
            );
        }

        return res;
    }

    public string Save(GameConfiguration data, string? id =  null)
    {
        if (!string.IsNullOrWhiteSpace(id)){
            if (!Guid.TryParse(id, out var guid))
                throw new ArgumentException("Invalid GUID format", nameof(id));
            var existing = _dbContext.GameConfigurations.FirstOrDefault(c => c.Id == guid);
            if (existing != null)
            {
                existing.Name = data.Name;
                existing.BoardWidth = data.BoardWidth;
                existing.BoardHeight = data.BoardHeight;
                existing.WinCondition = data.WinCondition;
                existing.IsBoardCylindrical = data.IsBoardCylindrical;

                _dbContext.Update(existing);
                _dbContext.SaveChanges();
                return existing.Id.ToString();
            }
        }

        _dbContext.GameConfigurations.Add(data);
        _dbContext.SaveChanges();
        return data.Id.ToString();    
    }
    
    public async Task<string> SaveAsync(GameConfiguration data, string? id = null)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            if (!Guid.TryParse(id, out var guid))
                throw new ArgumentException("Invalid GUID format", nameof(id));

            var existing = await _dbContext.GameConfigurations.FirstOrDefaultAsync(c => c.Id == guid);
            if (existing != null)
            {
                existing.Name = data.Name;
                existing.BoardWidth = data.BoardWidth;
                existing.BoardHeight = data.BoardHeight;
                existing.WinCondition = data.WinCondition;
                existing.IsBoardCylindrical = data.IsBoardCylindrical;

                _dbContext.Update(existing);
                await _dbContext.SaveChangesAsync();
                return existing.Id.ToString();
            }
        }

        await _dbContext.GameConfigurations.AddAsync(data);
        await _dbContext.SaveChangesAsync();
        return data.Id.ToString();
    }

    public GameConfiguration Load(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(id));
        
        var config = _dbContext.GameConfigurations.FirstOrDefault(c => c.Id == guid);
        if (config == null)
            throw new KeyNotFoundException($"Game configuration with ID '{id}' not found.");

        return config;
    }
    
    public async Task<GameConfiguration> LoadAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(id));

        var config = await _dbContext.GameConfigurations.FirstOrDefaultAsync(c => c.Id == guid);
        if (config == null)
            throw new KeyNotFoundException($"Game configuration with ID '{id}' not found.");

        return config;
    }

    public void Delete(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(id));
        
        var config = _dbContext.GameConfigurations.FirstOrDefault(c => c.Id == guid);
        if (config != null)
        {
            _dbContext.GameConfigurations.Remove(config);
            _dbContext.SaveChanges();
        }
    }
    
    public async Task DeleteAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
            throw new ArgumentException("Invalid GUID format", nameof(id));

        var config = await _dbContext.GameConfigurations.FirstOrDefaultAsync(c => c.Id == guid);
        if (config != null)
        {
            _dbContext.GameConfigurations.Remove(config);
            await _dbContext.SaveChangesAsync();
        }
    }
}