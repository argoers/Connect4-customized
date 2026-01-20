using System.ComponentModel.DataAnnotations;
using BLL;
using DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages;

public class CreateNewConfiguration : PageModel
{
    private readonly IConfigRepository<GameConfiguration> _configRepo;

    public CreateNewConfiguration(IConfigRepository<GameConfiguration> configRepo)
    {
        _configRepo = configRepo;
    }

    [BindProperty]
    [Range(3, 12, ErrorMessage = "Number of columns must be between 3 and 12")]
    public int Columns { get; set; } = 7;

    [BindProperty]
    [Range(3, 12, ErrorMessage = "Number of rows must be between 3 and 12")]
    public int Rows { get; set; } = 6;
    
    [BindProperty]
    public int WinCondition { get; set; } = 4;

    [BindProperty]
    public bool IsCylindrical { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Validate that win condition is reasonable for the board size
        if (WinCondition > Columns && WinCondition > Rows)
        {
            ModelState.AddModelError(
                nameof(WinCondition),
                "Win condition must be smaller than or equal to columns or rows."
            );
            return Page();
        }

        // Create new configuration
        var config = new GameConfiguration(Columns, Rows, WinCondition, IsCylindrical);
        await _configRepo.SaveAsync(config);

        // Redirect back to index page
        return RedirectToPage("./Index");
    }
}