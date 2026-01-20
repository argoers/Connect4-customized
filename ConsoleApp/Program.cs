using BLL;
using ConsoleApp;
using ConsoleUI;
using DAL;
using MenuSystem;
using Microsoft.EntityFrameworkCore;

IConfigRepository<GameConfiguration> configRepo;
IGameRepository<GameBrain> gameRepo;
IAiService aiService;

// Choose ONE!
//configRepo = new ConfigRepositoryJson();
//gameRepo = new GameRepositoryJson();

using var dbContext = GetDbContext();
configRepo = new ConfigRepositoryEf(dbContext);
gameRepo = new GameRepositoryEf(dbContext);

aiService = new AiService();

var menu0 = new Menu("Connect4 (customized) Main Menu", EMenuLevel.Root);

var configMenu = new ConfigurationMenuController(configRepo, gameRepo, aiService);
menu0.AddMenuItem("g", "Game Configurations", () => configMenu.BuildMenu().Run());

var gameMenu = new GameMenuController(gameRepo, aiService);
menu0.AddMenuItem("s", "Saved games", () => gameMenu.BuildMenu().Run());

menu0.Run();


Console.WriteLine("Game over!");

AppDbContext GetDbContext()
{
    // ========================= DB STUFF ========================
    var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    homeDirectory = homeDirectory + Path.DirectorySeparatorChar;

// We are using SQLite
    var connectionString = $"Data Source={homeDirectory}app.db";

    var contextOptions = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite(connectionString)
        .EnableDetailedErrors()
        .EnableSensitiveDataLogging()
        //.LogTo(Console.WriteLine)
        .Options;

    var dbContext = new AppDbContext(contextOptions);
    
    // apply any pending migrations (recreates db as needed)
    dbContext.Database.Migrate();
    
    return dbContext;
}
