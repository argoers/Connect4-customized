using BLL;
using DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages;

public class IndexModel : PageModel
{
    private readonly IConfigRepository<GameConfiguration> _configRepo;
    private readonly IGameRepository<GameBrain> _gameRepo;

    public IndexModel(IConfigRepository<GameConfiguration> configRepo, IGameRepository<GameBrain> gameRepo)
    {
        _configRepo = configRepo;
        _gameRepo = gameRepo;
    }

    public List<(string id, string description)> Configurations { get; set; } = default!;
    public List<(string id, string description, string date, bool isFinished)> Games { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Configurations = await _configRepo.ListAsync();
        Games = await _gameRepo.ListAsync();
    }
    
    public async Task<IActionResult> OnPostDeleteGameAsync(string id)
    {
        await _gameRepo.DeleteAsync(id);
        Configurations = await _configRepo.ListAsync();
        Games = await _gameRepo.ListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteConfAsync(string id)
    {
        try
        {
            await _configRepo.DeleteAsync(id);
        }
        catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException || ex is Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // Configuration has dependent games, cannot delete
            TempData["ErrorMessage"] = "Cannot delete configuration because it has games associated with it.";
        }

        // Re-initialize the data after deletion (or failed deletion)
        Configurations = await _configRepo.ListAsync();
        Games = await _gameRepo.ListAsync();
        return Page();
    }
}