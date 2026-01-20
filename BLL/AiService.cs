using System;

namespace BLL;

public class AiService : IAiService
{
    // Evaluation weights tuned for minimax.
    private const int WinScore = 1_000_000;
    private const int LoseScore = -1_000_000;

    // Playable immediate threats (3-in-a-row + playable empty).
    private const int ThreatKMinus1 = 50_000;
    private const int ThreatKMinus1Opp = 60_000; // slightly larger to bias blocking

    // Non-immediate, clean windows (no opponent discs).
    private const int TwoInRow = 400;
    private const int OneInRow = 40;

    // Small positional tie-breaker.
    private const int CenterWeight = 6;

    public int CalculateBestMove(GameBrain gameBrain, EAiDifficulty difficulty)
    {
        // In this project Blue starts. NextMoveByBlue tells whose turn it is now.
        bool aiIsBlue = gameBrain.NextMoveByBlue;
        var aiColor = aiIsBlue ? ECellState.Blue : ECellState.Red;
        var oppColor = aiIsBlue ? ECellState.Red : ECellState.Blue;

        // Quick tactical checks: win now, or block opponent win now.
        int winMove = GetWinningMove(gameBrain, aiColor);
        if (winMove != -1) return winMove; // return 0-based column

        int blockMove = GetWinningMove(gameBrain, oppColor);
        if (blockMove != -1) return blockMove;

        int depth = GetSearchDepth(difficulty);

        int bestScore = int.MinValue;
        int bestMove = -1;

        foreach (int col in GetMoveOrder(gameBrain.GameConfiguration.BoardWidth))
        {
            if (gameBrain.GetFreeSpaceInColumn(col + 1) == -1) continue;

            var afterMove = SimulateMove(gameBrain, col, aiIsBlue);
            int score = Minimax(afterMove, depth - 1, int.MinValue, int.MaxValue, false, aiIsBlue);

            // Tie-breaker to avoid always picking the first equal move.
            if (score > bestScore || (score == bestScore && Random.Shared.Next(2) == 0))
            {
                bestScore = score;
                bestMove = col;
            }
        }

        // Fallback: if no move found (should not happen), return 0.
        return bestMove >= 0 ? bestMove : 0;
    }

    private int Minimax(GameBrain gameBrain, int depth, int alpha, int beta, bool isMaximizing, bool aiIsBlue)
    {
        // Terminal or depth cutoff.
        if (depth == 0 || gameBrain.IsGameFinished)
            return EvaluateBoard(gameBrain, aiIsBlue);

        if (isMaximizing)
        {
            int best = int.MinValue;

            foreach (int col in GetMoveOrder(gameBrain.GameConfiguration.BoardWidth))
            {
                if (gameBrain.GetFreeSpaceInColumn(col + 1) == -1) continue;

                var child = SimulateMove(gameBrain, col, aiIsBlue);
                int val = Minimax(child, depth - 1, alpha, beta, false, aiIsBlue);

                if (val > best) best = val;
                if (val > alpha) alpha = val;

                if (beta <= alpha) break;
            }

            return best;
        }
        else
        {
            int best = int.MaxValue;

            foreach (int col in GetMoveOrder(gameBrain.GameConfiguration.BoardWidth))
            {
                if (gameBrain.GetFreeSpaceInColumn(col + 1) == -1) continue;

                // Opponent moves are the opposite color of AI.
                var child = SimulateMove(gameBrain, col, !aiIsBlue);
                int val = Minimax(child, depth - 1, alpha, beta, true, aiIsBlue);

                if (val < best) best = val;
                if (val < beta) beta = val;

                if (beta <= alpha) break;
            }

            return best;
        }
    }

    private static int[] GetMoveOrder(int width)
    {
        // Center-out ordering for any width.
        var order = new int[width];
        int center = width / 2;

        int idx = 0;
        order[idx++] = center;

        for (int offset = 1; idx < width; offset++)
        {
            int left = center - offset;
            if (left >= 0) order[idx++] = left;

            int right = center + offset;
            if (right < width) order[idx++] = right;
        }

        return order;
    }

    private GameBrain SimulateMove(GameBrain original, int column, bool moveIsBlue)
    {
        // Deep copy of game state (board must be copied).
        var simulated = new GameBrain
        {
            GameConfiguration = original.GameConfiguration,
            Player1Name = original.Player1Name,
            Player2Name = original.Player2Name,
            NextMoveByBlue = original.NextMoveByBlue,
            IsGameFinished = original.IsGameFinished,
            GameStatus = original.GameStatus,
            Winner = original.Winner
        };

        simulated.GameBoard = new ECellState[
            original.GameConfiguration.BoardWidth,
            original.GameConfiguration.BoardHeight
        ];
        Array.Copy(original.GameBoard, simulated.GameBoard, original.GameBoard.Length);

        int row0 = original.GetFreeSpaceInColumn(column + 1) - 1; // 0-based row index
        if (row0 < 0)
            return simulated; // invalid move, return unchanged copy

        var color = moveIsBlue ? ECellState.Blue : ECellState.Red;
        simulated.GameBoard[column, row0] = color;

        // Decide if the move ended the game.
        var winner = simulated.GetWinner(column, row0);
        if (winner != ECellState.Empty)
        {
            simulated.IsGameFinished = true;
            simulated.GameStatus = winner;
            simulated.Winner = winner == ECellState.Blue ? simulated.Player1Name : simulated.Player2Name;
        }
        else if (!simulated.AreThereFreeSpacesInGame())
        {
            simulated.IsGameFinished = true;
            simulated.GameStatus = ECellState.Draw;
        }
        else
        {
            simulated.NextMoveByBlue = !simulated.NextMoveByBlue;
        }

        return simulated;
    }

    public int EvaluateBoard(GameBrain g, bool aiIsBlue)
    {
        var ai = aiIsBlue ? ECellState.Blue : ECellState.Red;
        var opp = aiIsBlue ? ECellState.Red : ECellState.Blue;

        // Terminal dominates everything.
        if (g.IsGameFinished)
        {
            if (g.GameStatus == ai) return WinScore;
            if (g.GameStatus == opp) return LoseScore;
            return 0;
        }

        int w = g.GameConfiguration.BoardWidth;
        int h = g.GameConfiguration.BoardHeight;

        int score = 0;

        // Small center preference (tie-breaker, not a main driver).
        int center = w / 2;
        for (int y = 0; y < h; y++)
        {
            if (g.GameBoard[center, y] == ai)
                score += CenterWeight;
        }

        score += ScoreAllWindows(g, ai, opp);
        return score;
    }

    private int ScoreAllWindows(GameBrain g, ECellState ai, ECellState opp)
    {
        int w = g.GameConfiguration.BoardWidth;
        int h = g.GameConfiguration.BoardHeight;
        int k = g.GameConfiguration.WinCondition;

        int total = 0;

        // 4 directions: →, ↓, ↘, ↙
        (int dx, int dy)[] dirs = { (1, 0), (0, 1), (1, 1), (-1, 1) };

        for (int y0 = 0; y0 < h; y0++)
            for (int x0 = 0; x0 < w; x0++)
            {
                foreach (var (dx, dy) in dirs)
                {
                    // For non-cyl, skip windows that would go out of bounds in X.
                    if (!g.GameConfiguration.IsBoardCylindrical)
                    {
                        int xEnd = x0 + (k - 1) * dx;
                        int yEnd = y0 + (k - 1) * dy;
                        if (xEnd < 0 || xEnd >= w) continue;
                        if (yEnd < 0 || yEnd >= h) continue;
                    }
                    else
                    {
                        // Even with cyl, Y must stay in bounds
                        int yEnd = y0 + (k - 1) * dy;
                        if (yEnd < 0 || yEnd >= h) continue;
                    }

                    total += ScoreWindow(g, ai, opp, x0, y0, dx, dy, k);
                }
            }

        return total;
    }
    private static int WrapX(int x, int w)
    {
        x %= w;
        return x < 0 ? x + w : x;
    }
    
    private static ECellState GetCell(GameBrain g, int x, int y)
    {
        int w = g.GameConfiguration.BoardWidth;
        if (g.GameConfiguration.IsBoardCylindrical)
            x = WrapX(x, w);
        var cell = g.GameBoard[x, y];
        return cell;
    }
    
    private int ScoreWindow(GameBrain g, ECellState ai, ECellState opp,
        int x0, int y0, int dx, int dy, int k)
    {
        int aiCount = 0, oppCount = 0, emptyCount = 0;
        int emptyX = -1, emptyY = -1;

        for (int i = 0; i < k; i++)
        {
            int x = x0 + i * dx;
            int y = y0 + i * dy;

            var v = GetCell(g, x, y);

            if (v == ai) aiCount++;
            else if (v == opp) oppCount++;
            else
            {
                emptyCount++;
                // store last empty for immediate-threat check
                emptyX = g.GameConfiguration.IsBoardCylindrical ? WrapX(x, g.GameConfiguration.BoardWidth) : x;
                emptyY = y;
            }
        }

        // Dead window
        if (aiCount > 0 && oppCount > 0) return 0;

        // AI-only
        if (oppCount == 0 && aiCount > 0)
        {
            if (aiCount == k - 1 && emptyCount == 1 && IsPlayable(g, emptyX, emptyY))
                return ThreatKMinus1;
            if (aiCount == k - 2 && emptyCount == 2)
                return TwoInRow;
            if (aiCount == 1 && emptyCount == k - 1)
                return OneInRow;
            return 0;
        }

        // Opp-only
        if (aiCount == 0 && oppCount > 0)
        {
            if (oppCount == k - 1 && emptyCount == 1 && IsPlayable(g, emptyX, emptyY))
                return -ThreatKMinus1Opp;
            if (oppCount == k - 2 && emptyCount == 2)
                return -TwoInRow;
            if (oppCount == 1 && emptyCount == k - 1)
                return -OneInRow;
        }

        return 0;
    }
    

    private bool IsPlayable(GameBrain g, int x0, int y0)
    {
        return g.GetFreeSpaceInColumn(x0 + 1) == y0 + 1;
    }

    public int GetSearchDepth(EAiDifficulty difficulty)
    {
        // With the time limit enabled in Minimax and center-first ordering, these work well.
        return difficulty switch
        {
            EAiDifficulty.Easy => 1,
            EAiDifficulty.Medium => 3,
            EAiDifficulty.Hard => 6,
        };
    }

    private int GetWinningMove(GameBrain gameBrain, ECellState color)
    {
        // Find a column where dropping "color" wins immediately.
        foreach (int col in GetMoveOrder(gameBrain.GameConfiguration.BoardWidth))
        {
            int row = gameBrain.GetFreeSpaceInColumn(col + 1) - 1;
            if (row < 0) continue;

            var simulated = new GameBrain
            {
                GameConfiguration = gameBrain.GameConfiguration,
                Player1Name = gameBrain.Player1Name,
                Player2Name = gameBrain.Player2Name,
                NextMoveByBlue = gameBrain.NextMoveByBlue,
                IsGameFinished = gameBrain.IsGameFinished,
                GameStatus = gameBrain.GameStatus,
                Winner = gameBrain.Winner
            };

            simulated.GameBoard = new ECellState[
                gameBrain.GameConfiguration.BoardWidth,
                gameBrain.GameConfiguration.BoardHeight
            ];
            Array.Copy(gameBrain.GameBoard, simulated.GameBoard, gameBrain.GameBoard.Length);

            simulated.GameBoard[col, row] = color;

            if (simulated.GetWinner(col, row) == color)
                return col;
        }

        return -1;
    }
}