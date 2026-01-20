using BLL;
using ConsoleUI;
using DAL;
using MenuSystem;

namespace ConsoleApp;

public class GameMenuController
{
    private readonly IGameRepository<GameBrain> _gameRepo;
    private readonly IAiService _aiService;

    public GameMenuController(IGameRepository<GameBrain> gameRepo, IAiService aiService)
    {
        _gameRepo = gameRepo;
        _aiService = aiService;
    }
    public Menu BuildMenu()
    {
        var menu = new Menu("Connect4 (customized) Configurations", EMenuLevel.First);

        menu.AddMenuItem("u", "Play unfinished games", () => LoadGames(false));
        menu.AddMenuItem("f", "Look up finished games", () => LoadGames(true));
        menu.AddMenuItem("d", "Delete games", () => Delete());
        return menu;
    }

    private string LoadGames(bool isFinished)
    {
        var data = _gameRepo.List().FindAll(item =>item.isFinished == isFinished);
        if (data.Count == 0)
        {
            Console.WriteLine("No games to load.");
            return "OK";
        }

        for (int i = 0; i < data.Count; i++)
            Console.WriteLine($"{i + 1}: {data[i].description}, {data[i].date}");
        
        Console.WriteLine("Select game to load, 0 to cancel:");
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
                    if (isFinished)
                    {
                        Console.Clear();
                        var gameBrain = _gameRepo.Load(data[x - 1].id);
                        Ui.DrawBoard(gameBrain.GetBoard());
                        Console.WriteLine($"Game: {gameBrain.GameConfiguration.Name}");
                        if (gameBrain.Winner != null)
                            if (gameBrain.GetCurrentPlayerType() == EPlayerType.Ai) 
                                Console.WriteLine($"Winner is {gameBrain.GetCurrentPlayerColor()}: {gameBrain.GetCurrentPlayerName()} ({gameBrain.GetCurrentPlayerType()}/{gameBrain.GetCurrentPlayerDifficulty()})");
                            else
                                Console.WriteLine($"Winner is {gameBrain.GetCurrentPlayerColor()}: {gameBrain.GetCurrentPlayerName()} ({gameBrain.GetCurrentPlayerType()})");
                        else
                            Console.WriteLine("Draw! No free space left");
                    }
                    else
                    {
                        var gameBrain = _gameRepo.Load(data[x - 1].id);
                        var controller = new GameController(_gameRepo, gameBrain, _aiService);
                        controller.GameLoop();
                    }
                    return "OK";
                }
            }
            Console.WriteLine("Invalid input. Try again.");
        }
    }
    
    private string Delete()
    {
        var gameData = _gameRepo.List();

        if (gameData.Count == 0)
        {
            Console.WriteLine("No games to delete.");
            return "OK";
        }

        for (int i = 0; i < gameData.Count; i++)
        {
            var isFinishedText = gameData[i].isFinished ? "Finished" : "Not finished";
            Console.WriteLine($"{i + 1}: {gameData[i].description} {gameData[i].date} ({isFinishedText})");
        }
            
        
        Console.WriteLine("Select game to delete, 0 to cancel:");
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

                if (x > 0 && x <= gameData.Count)
                {
                    _gameRepo.Delete(gameData[x - 1].id);
                    Console.Clear();
                    return "OK";
                }
            }
            Console.WriteLine("Invalid input. Try again.");
        }
    }
}