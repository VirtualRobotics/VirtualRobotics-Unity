using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Configuration")]
    public string gameSceneName = "GameScene";
    public GameObject infoPanel;

    public void ToggleInfoPanel(bool show)
    {
        infoPanel.SetActive(show);
    }

    public void StartHeuristicMode()
    {
        Debug.Log("Mode chosen: Heuristic");
        
        GameSettings.CurrentMode = GameSettings.GameMode.HeuristicCV;
        
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartRLMode()
    {
        Debug.Log("Mode chosen: RL");
        
        GameSettings.CurrentMode = GameSettings.GameMode.ReinforcementLearning;
        
        SceneManager.LoadScene(gameSceneName);
    }
    
}