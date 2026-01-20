using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Konfiguracja")]
    public string gameSceneName = "GameScene";
    public GameObject infoPanel;

    public void ToggleInfoPanel(bool show)
    {
        infoPanel.SetActive(show);
    }

    public void StartHeuristicMode()
    {
        Debug.Log("Wybrano tryb: Heurystyka (Python/CV)");
        
        GameSettings.CurrentMode = GameSettings.GameMode.HeuristicCV;
        
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartRLMode()
    {
        Debug.Log("Wybrano tryb: AI Learning (RL)");
        
        GameSettings.CurrentMode = GameSettings.GameMode.ReinforcementLearning;
        
        SceneManager.LoadScene(gameSceneName);
    }
    
}