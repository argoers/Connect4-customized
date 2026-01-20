using BLL;
using ConsoleUI;
using DAL;

namespace ConsoleApp;

public class GameController
{
    private IGameRepository<GameBrain> _gameRepo;
    private IAiService? _aiService;
    private GameBrain GameBrain { get; set; }
    
    public GameController(IGameRepository<GameBrain> gameRepo, GameBrain gameBrain, IAiService aiService)
    {
        _gameRepo = gameRepo;
        GameBrain = gameBrain;
        _aiService = aiService;
    }

    public GameController(IGameRepository<GameBrain> gameRepo, GameConfiguration config, string player1Name, EPlayerType player1Type, EAiDifficulty? player1Difficulty, string player2Name, EPlayerType player2Type, EAiDifficulty? player2Difficulty, IAiService aiService)
    {
        _gameRepo = gameRepo;
        GameBrain = new GameBrain(config, player1Name, player2Name, player1Type, player2Type, player1Difficulty, player2Difficulty);
        _aiService = aiService;
    }

    public void GameLoop()
    {
        var gameOver = false;
        do
        {
            Console.Clear();

            Ui.DrawBoard(GameBrain.GetBoard());
            Ui.ShowNextPlayer(GameBrain);
            
            // Show AI thinking indicator if it's AI's turn
            if (GameBrain.IsCurrentPlayerAi())
            {
                Console.WriteLine("AI is thinking... (press X to save & exit)");
                if (_aiService != null && (GameBrain.Player1Difficulty != null || GameBrain.Player2Difficulty != null))
                {
                    // Run AI in the background so we can still read input
                    var aiTask = Task.Run(() => GameBrain.ProcessAiMoveWithCoordinatesSafe(_aiService));

                    // Poll keyboard while AI is running
                    while (!aiTask.IsCompleted)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(intercept: true);
                            if (key.Key == ConsoleKey.X)
                            {
                                GameBrain.InvalidateAi();    // bumps AiGeneration
                                _gameRepo.Save(GameBrain);   // save last stable state
                                return;                      // exit immediately
                            }
                        }

                        Thread.Sleep(25); // small throttle
                    }

                    // AI finished; apply result (null means it was cancelled)
                    var move = aiTask.Result;
                    if (move == null)
                    {
                        // cancelled -> we already saved & returned above, but just in case:
                        continue;
                    }

                    if (GameBrain.IsGameFinished)
                    {
                        ShowGameEndScreen();
                        break;
                    }

                    continue; // skip human input, AI already moved
                }
            }
            
            Console.WriteLine("Choose column");
            Console.Write("or exit game and save it (x): ");

            var input = Console.ReadLine();
            
            if (input == null) continue;
            input = input.ToLower();

            if (input == "x")
            {
                gameOver = true;
                continue;
            }

            if (int.TryParse(input, out var x))
            {
                try
                {
                    int y = GameBrain.GetFreeSpaceInColumn(x);
                    
                    GameBrain.ProcessMove(x - 1, y - 1);

                    if (GameBrain.IsGameFinished)
                    {
                        ShowGameEndScreen();
                        break;
                    }
                }
                catch (Exception e) {}
            }
                
        } while (!gameOver);
        _gameRepo.Save(GameBrain);

    }
    
    private void ShowGameEndScreen()
    {
        Console.Clear();
        Ui.DrawBoard(GameBrain.GetBoard());

        if (GameBrain.Winner != null)
            if (GameBrain.GetCurrentPlayerType() == EPlayerType.Ai) 
                Console.WriteLine($"Winner is {GameBrain.GetCurrentPlayerColor()}: {GameBrain.GetCurrentPlayerName()} ({GameBrain.GetCurrentPlayerType()}/{GameBrain.GetCurrentPlayerDifficulty()})");
            else
                Console.WriteLine($"Winner is {GameBrain.GetCurrentPlayerColor()}: {GameBrain.GetCurrentPlayerName()} ({GameBrain.GetCurrentPlayerType()})");
        else
            Console.WriteLine("Draw! No free space left");

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }
}