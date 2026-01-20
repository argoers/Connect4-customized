using BLL;
using DAL;
using MenuSystem;

namespace ConsoleApp;

public class ConfigurationMenuController
{
    private readonly IConfigRepository<GameConfiguration> _configRepo;
    private readonly IGameRepository<GameBrain> _gameRepo;
    private readonly IAiService _aiService;

    public ConfigurationMenuController(IConfigRepository<GameConfiguration> configRepo,  IGameRepository<GameBrain> gameRepo, IAiService aiService)
    {   
        _configRepo = configRepo;
        _gameRepo = gameRepo;
        _aiService = aiService;
    }

    public Menu BuildMenu()
    {
        var menu = new Menu("Connect4 (customized) Configurations", EMenuLevel.First);

        menu.AddMenuItem("l", "Load/Play", Load);
        menu.AddMenuItem("c", "Create", Create);
        menu.AddMenuItem("d", "Delete", Delete);

        return menu;
    }

    private string Load()
    {
        var data = _configRepo.List();
        if (data.Count == 0)
        {
            Console.WriteLine("No configurations to load.");
            return "OK";
        }

        for (int i = 0; i < data.Count; i++)
            Console.WriteLine($"{i + 1}: {data[i].description}");
        
        Console.WriteLine("Select config to load, 0 to cancel:");
        while (true)
        {
            var userChoice = Console.ReadLine();
            if (int.TryParse(userChoice, out var x))
            {
                if (x == 0)
                {
                    Console.Clear();
                    return "OK";
                }
                if (x > 0 && x <= data.Count)
                {
                    var config = _configRepo.Load(data[x - 1].id);
                    Console.Clear();

                    string player1Name;
                    EPlayerType player1Type;
                    EAiDifficulty? player1Difficulty = null;

                    string player2Name;
                    EPlayerType player2Type;
                    EAiDifficulty? player2Difficulty = null;

                    while (true)
                    {
                        Console.Write("Enter player 1 name: ");
                        player1Name = (Console.ReadLine() ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(player1Name))
                        {
                            Console.WriteLine("Name cannot be empty. Try again.");
                        }
                        else
                        {
                            break;
                        }
                    }

                    while (true) 
                    {
                        Console.Write("Enter player 1 type (Human/AI): ");
                        var player1TypeChoice = (Console.ReadLine() ?? "").Trim();

                        if (player1TypeChoice.Equals("Human", StringComparison.OrdinalIgnoreCase))
                        {
                            player1Type = EPlayerType.Human;
                            break;
                        }
                            
                        if (player1TypeChoice.Equals("AI", StringComparison.OrdinalIgnoreCase)){
                            player1Type = EPlayerType.Ai;
                            player1Difficulty = ReadDifficulty("Choose player 1 AI difficulty (Easy/Medium/Hard): ");
                            break;
                        }
                        Console.WriteLine("Invalid input. Try again.");
                    }

                    while (true)
                    {
                        Console.Write("Enter player 2 name: ");
                        player2Name = (Console.ReadLine() ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(player2Name))
                        {
                            Console.WriteLine("Name cannot be empty. Try again.");
                        }
                        else
                        {
                            break;
                        }
                    }

                    while (true)
                    {
                        Console.Write("Enter player 2 type (Human/AI): ");
                        var player2TypeChoice = (Console.ReadLine() ?? "").Trim();

                        if (player2TypeChoice.Equals("Human", StringComparison.OrdinalIgnoreCase))
                        {
                            player2Type = EPlayerType.Human;
                            break;
                        }
                        
                        if (player2TypeChoice.Equals("AI", StringComparison.OrdinalIgnoreCase))
                        {
                            player2Type = EPlayerType.Ai;
                            player2Difficulty = ReadDifficulty("Choose player 2 AI difficulty (Easy/Medium/Hard): ");
                            break;
                        }
                        Console.WriteLine("Invalid input. Try again.");
                    }

                    var controller = new GameController(
                        _gameRepo,
                        config,
                        player1Name,
                        player1Type,
                        player1Difficulty,
                        player2Name,
                        player2Type,
                        player2Difficulty,
                        _aiService
                    );
                    controller.GameLoop();
                    return MenuDefaults.MainMenuKey;
                }
            }
            Console.WriteLine("Invalid input. Try again.");
        }
    }
    
    
    private static EAiDifficulty ReadDifficulty(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var s = (Console.ReadLine() ?? "").Trim();

            if (s.Equals("Easy", StringComparison.OrdinalIgnoreCase)) return EAiDifficulty.Easy;
            if (s.Equals("Medium", StringComparison.OrdinalIgnoreCase)) return EAiDifficulty.Medium;
            if (s.Equals("Hard", StringComparison.OrdinalIgnoreCase)) return EAiDifficulty.Hard;

            Console.WriteLine("Invalid difficulty. Enter Easy/Medium/Hard.");
        }
    }

    private string Create()
    {
        _configRepo.Save(GameConfiguration.CreateFromUserInput());
        Console.Clear();
        return "OK";
    }

    private string Delete()
    {
        var configData = _configRepo.List();

        if (configData.Count == 0)
        {
            Console.WriteLine("No configurations to delete.");
            return "OK";
        }

        for (int i = 0; i < configData.Count; i++)
            Console.WriteLine($"{i + 1}: {configData[i].description}");
        
        Console.WriteLine("Select config to delete, 0 to cancel:");
        while (true)
        {
            var userChoice = Console.ReadLine();
            if (int.TryParse(userChoice, out var x))
            {
                if (x == 0)
                {
                    Console.Clear();
                    return "OK";
                }

                if (x > 0 && x <= configData.Count)
                {
                    var gameData = _gameRepo.List();
                    var configInGame = false;
                    foreach (var game in gameData)
                    {
                        if (game.description == configData[x - 1].description)
                        {
                            Console.WriteLine("Selected configuration is connected to some saved game. Cannot be deleted.");
                            configInGame = true;
                            break;
                        }
                    }

                    if (configInGame) continue;
                    
                    _configRepo.Delete(configData[x - 1].id);
                    Console.Clear();
                    return "OK";
                }
            }
            Console.WriteLine("Invalid input. Try again.");
        }
    }
}