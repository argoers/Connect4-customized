using System.ComponentModel.DataAnnotations;
using BLL;
using DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages;

public class NewGame : PageModel
{
    private readonly IConfigRepository<GameConfiguration> _configRepo;
    private readonly IGameRepository<GameBrain> _gameRepo;

    public NewGame(IConfigRepository<GameConfiguration> configRepo, IGameRepository<GameBrain> gameRepo)
    {
        _configRepo = configRepo;
        _gameRepo = gameRepo;
    }
    
    public string SelectedConfigurationDescription { get; set; } = default!;

    [BindProperty]
    public string ConfigId { get; set; } = default!;
    
    [BindProperty]
    [Length(3,32)]
    public string Player1Name { get; set; } = default!;
    
    [BindProperty]
    [Length(3,32)]
    public string Player2Name { get; set; } = default!;
    
    [BindProperty]
    public EPlayerType Player1Type { get; set; }
    
    [BindProperty]
    public EPlayerType Player2Type { get; set; }
    
    [BindProperty]
    public EAiDifficulty? Player1Difficulty { get; set; }

    [BindProperty]
    public EAiDifficulty? Player2Difficulty { get; set; }
    
    public async Task OnGetAsync(string? configId)
    {
        // Only set defaults on initial GET, not when returning from validation failure
        // Check if this is a fresh page load (no form submission involved)
        if (!HttpContext.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
            ModelState.IsValid &&
            string.IsNullOrEmpty(Player1Name) &&
            string.IsNullOrEmpty(Player2Name))
        {
            // Set default values only on initial page load
            Player1Type = EPlayerType.Human;
            Player2Type = EPlayerType.Human;
            Player1Difficulty = EAiDifficulty.Medium;
            Player2Difficulty = EAiDifficulty.Medium;
        }

        // If configId is provided in the URL, set it as the selected configuration
        if (!string.IsNullOrEmpty(configId))
        {
            ConfigId = configId;
            var config = await _configRepo.LoadAsync(configId);
            SelectedConfigurationDescription = config.Name;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var config = await _configRepo.LoadAsync(ConfigId);
            SelectedConfigurationDescription = config.Name;
            return Page();
        }
        
        // Create new game and save it to database
         var configToUse = await _configRepo.LoadAsync(ConfigId);

        var gameBrain = new GameBrain(configToUse, Player1Name, Player2Name)
        {
            Player1Type = Player1Type,
            Player2Type = Player2Type,
            Player1Difficulty = Player1Difficulty,
            Player2Difficulty = Player2Difficulty
        };
        var gameId = await _gameRepo.SaveAsync(gameBrain);
        
        return RedirectToPage("./GamePlay", new { id = gameId , player1Name = Player1Name, player2Name = Player2Name });
    }
}