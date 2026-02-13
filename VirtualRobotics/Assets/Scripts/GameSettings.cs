public static class GameSettings
{
    public enum GameMode 
    { 
        HeuristicCV,
        ReinforcementLearning,
        Training
    }

    public static GameMode CurrentMode = GameMode.HeuristicCV;
    
    public static int MazeWidth = 11;
    public static int MazeHeight = 11;
}