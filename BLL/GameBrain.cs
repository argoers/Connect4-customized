using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BLL;

public class GameBrain : BaseEntity
{
    public string GameBoardJson { get; set; } = default!;

    [JsonIgnore]
    [NotMapped]
    public ECellState[,] GameBoard { get; set; } = default!;

    public Guid GameConfigurationId { get; set; }
    public GameConfiguration GameConfiguration { get; set; } = default!;

    public string Player1Name { get; set; } = default!;
    public string Player2Name { get; set; } = default!;

    public bool NextMoveByBlue { get; set; } = true;
    public bool IsGameFinished { get; set; }

    public ECellState GameStatus { get; set; } = ECellState.Empty;
    public string? Winner { get; set; }

    public EPlayerType Player1Type { get; set; } = EPlayerType.Human;
    public EPlayerType Player2Type { get; set; } = EPlayerType.Human;
    public EAiDifficulty? Player1Difficulty { get; set; }
    public EAiDifficulty? Player2Difficulty { get; set; }

    // Track last move for animation support (optional)
    public int LastMoveColumn { get; set; } = -1;
    public int LastMoveRow { get; set; } = -1;
    
    [JsonIgnore]
    [NotMapped]
    public int AiGeneration { get; set; }

    public GameBrain() { }

    public GameBrain(GameConfiguration configuration, string player1Name, string player2Name)
    {
        GameConfiguration = configuration;
        Player1Name = player1Name;
        Player2Name = player2Name;
        GameBoard = new ECellState[configuration.BoardWidth, configuration.BoardHeight];
    }

    public GameBrain(
        GameConfiguration configuration,
        string player1Name,
        string player2Name,
        EPlayerType player1Type,
        EPlayerType player2Type,
        EAiDifficulty? player1Difficulty,
        EAiDifficulty? player2Difficulty)
        : this(configuration, player1Name, player2Name)
    {
        Player1Type = player1Type;
        Player2Type = player2Type;
        Player1Difficulty = player1Difficulty;
        Player2Difficulty = player2Difficulty;
    }

    public void SyncToJson()
    {
        // Convert 2D array to jagged array
        var width = GameBoard.GetLength(0);
        var height = GameBoard.GetLength(1);

        var jagged = new ECellState[width][];
        for (int i = 0; i < width; i++)
        {
            jagged[i] = new ECellState[height];
            for (int j = 0; j < height; j++)
            {
                jagged[i][j] = GameBoard[i, j];
            }
        }

        GameBoardJson = JsonSerializer.Serialize(jagged);
    }

    public void SyncFromJson()
    {
        if (string.IsNullOrWhiteSpace(GameBoardJson))
        {
            GameBoard = new ECellState[GameConfiguration.BoardWidth, GameConfiguration.BoardHeight];
            return;
        }

        // Deserialize jagged array and convert back to 2D
        var jagged = JsonSerializer.Deserialize<ECellState[][]>(GameBoardJson)!;
        var width = jagged.Length;
        var height = jagged[0].Length;

        GameBoard = new ECellState[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                GameBoard[i, j] = jagged[i][j];
            }
        }
    }
    
    public ECellState[,] GetBoard()
    {
        var gameBoardCopy = new ECellState[GameConfiguration.BoardWidth, GameConfiguration.BoardHeight];
        Array.Copy(GameBoard, gameBoardCopy, GameBoard.Length);
        return gameBoardCopy;
    }
    
    public bool IsNextPlayerBlue() => NextMoveByBlue;

    public string GetCurrentPlayerColor() => NextMoveByBlue ? "Blue" : "Red";
    public string GetCurrentPlayerName() => NextMoveByBlue ? Player1Name : Player2Name;
    public EPlayerType GetCurrentPlayerType() => NextMoveByBlue ? Player1Type : Player2Type;
    public EAiDifficulty? GetCurrentPlayerDifficulty() => NextMoveByBlue ? Player1Difficulty : Player2Difficulty;

    public bool IsGameActive() => !IsGameFinished;

    public bool IsCurrentPlayerAi()
    {
        return NextMoveByBlue
            ? Player1Type == EPlayerType.Ai
            : Player2Type == EPlayerType.Ai;
    }

    public int GetFreeSpaceInColumn(int x)
    {
        for (int y = GameConfiguration.BoardHeight; y > 0; y--)
        {
            if (GameBoard[x-1, y-1] == ECellState.Empty) return y;
        }

        return -1;
    }
    
    public bool AreThereFreeSpacesInGame()
    {
        for (int x = 1; x <= GameConfiguration.BoardWidth; x++)
        {
            if (GetFreeSpaceInColumn(x) != -1) return true;
        }

        return false;
    }

    public bool CheckForDraw() => !AreThereFreeSpacesInGame() && !IsGameFinished;
    
    public void InvalidateAi() => AiGeneration++;
    public async Task<(int column, int row)> ProcessAiMoveWithCoordinates(IAiService aiService)
    {
        var diff = NextMoveByBlue ? Player1Difficulty : Player2Difficulty;
        int bestColumn = aiService.CalculateBestMove(this, diff!.Value);
        int row = GetFreeSpaceInColumn(bestColumn + 1) - 1;

        ProcessMove(bestColumn, row);
        return (bestColumn, row);
    }
    
    public Task<(int column, int row)?> ProcessAiMoveWithCoordinatesSafe(IAiService aiService)
    {
        var myGen = AiGeneration;

        var diff = NextMoveByBlue ? Player1Difficulty : Player2Difficulty;
        if (diff == null) throw new InvalidOperationException("AI difficulty is not set.");

        int bestColumn = aiService.CalculateBestMove(this, diff.Value);

        // user cancelled while AI was running -> ignore move
        if (myGen != AiGeneration) return Task.FromResult<(int, int)?>(null);

        int row = GetFreeSpaceInColumn(bestColumn + 1) - 1;

        // re-check before mutating state
        if (myGen != AiGeneration) return Task.FromResult<(int, int)?>(null);

        ProcessMove(bestColumn, row);
        return Task.FromResult<(int, int)?>((bestColumn, row));
    }

    public void ProcessMove(int x, int y)
    {
        if (GameBoard[x, y] != ECellState.Empty) return;

        GameBoard[x, y] = NextMoveByBlue ? ECellState.Blue : ECellState.Red;

        LastMoveColumn = x;
        LastMoveRow = y;

        var winner = GetWinner(x, y);
        if (winner != ECellState.Empty)
        {
            IsGameFinished = true;
            GameStatus = winner;
            Winner = winner == ECellState.Blue ? Player1Name : Player2Name;
            return;
        }

        if (!AreThereFreeSpacesInGame())
        {
            IsGameFinished = true;
            GameStatus = ECellState.Draw;
            Winner = null;
            return;
        }

        NextMoveByBlue = !NextMoveByBlue;
    }

    private (int dirX, int dirY) GetDirection(int directionCount) =>
        directionCount switch
        {
            0 => (-1, -1),
            1 => (0, -1),
            2 => (1, -1),
            3 => (1, 0),
            _ => (0, 0)
        };
    
    private (int dirX, int dirY) FlipDirection((int dirX, int dirY) direction) => 
        (-direction.dirX, -direction.dirY);

    public bool BoardCoordinatesAreValid(int x, int y)
    {
        if (x < 0 || x >= GameConfiguration.BoardWidth) return false;
        if (y < 0 || y >= GameConfiguration.BoardHeight) return false;
        return true;
    }
    public ECellState GetWinner(int x, int y)
    {
        if (GameBoard[x, y] == ECellState.Empty) return ECellState.Empty;
        int width = GameBoard.GetLength(0);

        for (int directionIndex = 0; directionIndex < 4; directionIndex++)
        {
            var (dirX, dirY) = GetDirection(directionIndex);
            var count = 0;

            var nextX = x;
            var nextY = y;
            while (BoardCoordinatesAreValid(nextX, nextY) && GameBoard[x,y] == GameBoard[nextX, nextY] && count <= GameConfiguration.WinCondition)
            {
                count++;
                nextX += dirX;
                nextY += dirY;
                
                if (IsBoardCylindrical())
                {
                    nextX = (nextX + width) % width;
                }
            }
            
            if (count < GameConfiguration.WinCondition)
            {
                (dirX, dirY) = FlipDirection((dirX, dirY));
                nextX = x + dirX;
                nextY = y + dirY;
                while (BoardCoordinatesAreValid(nextX, nextY) && GameBoard[x,y] == GameBoard[nextX, nextY] && count <= GameConfiguration.WinCondition)
                {
                    count++;
                    nextX += dirX;
                    nextY += dirY;
                    
                    if (IsBoardCylindrical())
                    {
                        nextX = (nextX + width) % width;
                    }
                }
            }
            
            if (count == GameConfiguration.WinCondition)
            {
                return GameBoard[x, y] == ECellState.Blue ? ECellState.Blue : ECellState.Red;
            }
        }
        return ECellState.Empty;
            
    }

    public bool IsBoardCylindrical() => GameConfiguration.IsBoardCylindrical;
}