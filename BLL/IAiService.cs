namespace BLL;

public interface IAiService
{
    int CalculateBestMove(GameBrain gameBrain, EAiDifficulty difficulty);
    int EvaluateBoard(GameBrain gameBrain, bool isAiPlayerBlue);
    int GetSearchDepth(EAiDifficulty difficulty);
}