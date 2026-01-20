using BLL;

namespace ConsoleUI;

public class Ui
{
    public static void ShowNextPlayer(GameBrain gameBrain)
    {
        if (gameBrain.GetCurrentPlayerType() == EPlayerType.Ai) 
            Console.WriteLine($"{gameBrain.GetCurrentPlayerColor()} turn: {gameBrain.GetCurrentPlayerName()} ({gameBrain.GetCurrentPlayerType()}/{gameBrain.GetCurrentPlayerDifficulty()})");
        else
            Console.WriteLine($"{gameBrain.GetCurrentPlayerColor()} turn: {gameBrain.GetCurrentPlayerName()} ({gameBrain.GetCurrentPlayerType()})");
    }
    public static void DrawBoard(ECellState[,] gameBoard)
    {
        Console.Write("   ");
        for (int x = 0; x < gameBoard.GetLength(0); x++)
        {
            Console.Write("|" + GetNumberRepresentation(x+1));
        }
        Console.WriteLine();
        
        for (int y = 0; y < gameBoard.GetLength(1); y++)
        {
            for (int x = 0; x < gameBoard.GetLength(0); x++)
            {
                Console.Write("---+");
            }
            
            Console.WriteLine("---");
            Console.Write(GetNumberRepresentation(y+1));
            
            for (int x = 0; x < gameBoard.GetLength(0); x++)
            {
                DrawSymbolWithColor(gameBoard[x, y]);
            }
            Console.WriteLine();
        }
    }

    private static string GetNumberRepresentation(int number)
    {
        return " " + (number < 10 ? "0" + number : number.ToString());
    }

    private static void DrawSymbolWithColor(ECellState cellValue)
    {
        Console.Write("|");
        if (cellValue == ECellState.Blue)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(" ● ");
            Console.ResetColor();
        }
        else if (cellValue == ECellState.Red)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(" ● ");
            Console.ResetColor();
        }
        else
        {
            Console.Write("   ");
        }
    }
}