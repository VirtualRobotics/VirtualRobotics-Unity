public static class GameSettings
{
    public enum GameMode 
    { 
        HeuristicCV,
        ReinforcementLearning
    }

    public static GameMode CurrentMode = GameMode.HeuristicCV;
}