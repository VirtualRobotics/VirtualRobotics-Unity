public static class EvaluationSettings
{
    public enum GameMode 
    { 
        HeuristicCV,
        ReinforcementLearning,

    }

    public static GameMode CurrentMode = GameMode.HeuristicCV;
    
    public static int MazeWidth = 11;
    public static int MazeHeight = 11;
    
    public static bool GenerateEmptyMaze = false;
    
    public static bool UseCustomSeed = false;
    public static int StartingSeed = 42;
}