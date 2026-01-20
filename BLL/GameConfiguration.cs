using Microsoft.EntityFrameworkCore;

namespace BLL;

[Index(nameof(BoardWidth), nameof(BoardHeight), nameof(WinCondition), nameof(IsBoardCylindrical), IsUnique = true)]
public class GameConfiguration : BaseEntity
{
    public string Name { get; set; } = default!;
    public int BoardWidth { get; set; }
    public int BoardHeight { get; set; }
    public int WinCondition { get; set; }
    public bool IsBoardCylindrical { get; set; }
    public GameConfiguration() { }

    public GameConfiguration(int boardWidth, int boardHeight, int winCondition, bool isBoardCylindrical)
    {
        BoardWidth = boardWidth;
        BoardHeight = boardHeight;
        WinCondition = winCondition;
        IsBoardCylindrical = isBoardCylindrical;
        Name = "Connect" + WinCondition;
        if (IsBoardCylindrical)
        {
            Name += "-cylinder";
        }

        Name += $" ({boardWidth}x{boardHeight})";
    }
    
    public static GameConfiguration CreateFromUserInput()
    {
        int x, y;
        do
        {
            Console.Write("Choose board dimensions (x,y): ");
            var input = Console.ReadLine();
            if (input == null) continue;
            var parts = input.Split(',');
            if (parts.Length == 2)
            { 
                if (int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y))
                {
                    if (x <= 0 || y <= 0)
                    {
                        Console.WriteLine("Both values must be positive.");
                    }
                    else if (x > 12 || y > 12 || x < 3 || y < 3)
                    {
                        Console.WriteLine("Both values must be between 3 and 12.");
                    }
                    else
                    {
                        break;   
                    }
                }
            }
            Console.WriteLine("Invalid input. Try again.");
        } while (true);

        int winCondition;
        do
        {
            Console.Write("Choose win condition: ");
            var input = Console.ReadLine();
            if (int.TryParse(input, out winCondition))
            {
                if (winCondition < 3)
                {
                    Console.WriteLine("Win condition must be at  least 3.");
                    continue;
                } 
                
                if (winCondition > x && winCondition > y)
                {
                    Console.WriteLine("Win condition must be smaller than or equal to width and height.");
                    continue;
                }
                break;
            }
            Console.WriteLine("Invalid input. Try again.");
        } while (true);
        
        do
        {
            Console.Write("Is board cylindrical? (y/n): ");
            var input = Console.ReadLine();
            
            if (input == null) continue;
            input = input.ToLower();

            if (input == "y")
            {
                Console.Clear();
                return new GameConfiguration(x,y,winCondition,true);
            }
            if (input == "n")
            {
                Console.Clear();
                return new GameConfiguration(x,y,winCondition,false);   
            }
            Console.WriteLine("Invalid input. Try again.");
        } while (true);
    }
}