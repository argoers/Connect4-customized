using BLL;
using DAL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages;

public class GamePlay : PageModel
{
    private readonly IGameRepository<GameBrain> _gameRepo;

    public GamePlay(IGameRepository<GameBrain> gameRepo)
    {
        _gameRepo = gameRepo;
    }

    public string GameId { get; set; } = default!;
    public GameBrain GameBrain { get; set; } = default!;

    public string CurrentPlayerName => GameBrain.GetCurrentPlayerName();
    public bool IsGameActive => GameBrain.IsGameActive();
    public string? Winner => GameBrain.Winner;

    public class MoveRequest
    {
        public string GameId { get; set; } = string.Empty;
        public int? Column { get; set; } // nullable for AI autoplay
    }

    public class MoveResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        public List<MoveInfo> Moves { get; set; } = new();

        public bool GameEnded { get; set; }
        public string? Winner { get; set; }
        public bool IsDraw { get; set; }

        public string CurrentPlayerName { get; set; } = "";
        public bool NextPlayerIsAi { get; set; }

        public bool ShouldAnimate { get; set; } = true;
    }

    public class MoveInfo
    {
        public int Column { get; set; }
        public int Row { get; set; }
        public string Color { get; set; } = "blue"; // "blue" or "red"
    }
    
    public async Task OnGetAsync(string id)
    {
        GameId = id;
        GameBrain = await _gameRepo.LoadAsync(id);
    }

    [ValidateAntiForgeryToken]
    public async Task<JsonResult> OnPostMakeMove(
        [FromBody] MoveRequest request,
        [FromServices] IAiService aiService)
    {
        try
        {
            var game = await _gameRepo.LoadAsync(request.GameId);

            if (!game.IsGameActive())
            {
                return new JsonResult(new MoveResponse
                {
                    Success = true,               
                    Message = "Game has already ended",
                    Moves = new List<MoveInfo>(), 
                    ShouldAnimate = false,
                    GameEnded = true,
                    Winner = game.Winner,
                    IsDraw = string.IsNullOrEmpty(game.Winner),
                    CurrentPlayerName = game.GetCurrentPlayerName(),
                    NextPlayerIsAi = false        
                });
            }

            var moves = new List<MoveInfo>();

            if (!game.IsCurrentPlayerAi())
            {
                if (!request.Column.HasValue)
                {
                    return new JsonResult(new MoveResponse
                    {
                        Success = false,
                        Message = "Column is required for a human move",
                        ShouldAnimate = false
                    });
                }

                var col = request.Column.Value;
                var row = game.GetFreeSpaceInColumn(col + 1) - 1;
                if (row < 0)
                {
                    return new JsonResult(new MoveResponse
                    {
                        Success = false,
                        Message = "Column is full",
                        ShouldAnimate = false
                    });
                }

                var color = game.IsNextPlayerBlue() ? "blue" : "red"; // BEFORE move
                game.ProcessMove(col, row);
                moves.Add(new MoveInfo { Column = col, Row = row, Color = color });
            }
            else
            {
                // AI turn => column must be null (autoplay tick)
                if (request.Column.HasValue)
                {
                    return new JsonResult(new MoveResponse
                    {
                        Success = false,
                        Message = "Not your turn - AI is thinking",
                        ShouldAnimate = false
                    });
                }
            }

            // Do exactly ONE AI move (NOT a while loop)
            if (game.IsGameActive() && game.IsCurrentPlayerAi())
            {
                var aiColor = game.IsNextPlayerBlue() ? "blue" : "red"; 
                var (aiCol, aiRow) = await game.ProcessAiMoveWithCoordinates(aiService);

                moves.Add(new MoveInfo { Column = aiCol, Row = aiRow, Color = aiColor });
            }

            await _gameRepo.SaveAsync(game);

            var ended = !game.IsGameActive();
            var winner = ended ? game.Winner : null;
            var isDraw = ended && string.IsNullOrEmpty(winner);

            return new JsonResult(new MoveResponse
            {
                Success = true,
                Moves = moves,
                GameEnded = ended,
                Winner = winner,
                IsDraw = isDraw,
                CurrentPlayerName = game.GetCurrentPlayerName(),
                NextPlayerIsAi = game.IsCurrentPlayerAi(),
                ShouldAnimate = true
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new MoveResponse
            {
                Success = false,
                Message = "Error processing move: " + ex.Message,
                ShouldAnimate = false
            });
        }
    }
}