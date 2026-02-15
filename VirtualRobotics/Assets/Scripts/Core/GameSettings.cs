public static class GameSettings
{
    public enum GameMode 
    { 
        HeuristicCV,
        ReinforcementLearning,
        Training,
        Manual
    }

    public static GameMode CurrentMode = GameMode.HeuristicCV;
    
    public static int MazeWidth = 11;
    public static int MazeHeight = 11;
    
    public static bool KeepMapLayout = false;
    public static bool GenerateEmptyMaze = false;
}