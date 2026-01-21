using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameUIController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI modeText;
    public string menuSceneName = "MenuScene";

    [Header("Agent References")]
    public RLAgentController rlAgent; 

    void Start()
    {
        UpdateModeText();
    }

    void UpdateModeText()
    {
        if (GameSettings.CurrentMode == GameSettings.GameMode.HeuristicCV)
        {
            modeText.text = "TRYB: HEURYSTYKA";
            modeText.color = new Color(1f, 1f, 1f);
        }
        else
        {
            modeText.text = "TRYB: RL";
            modeText.color = new Color(1f, 1f, 1f);
        }
    }

    public void OnResetClicked()
    {
        Debug.Log("Wymuszony reset poziomu.");
    
        if (GameSettings.CurrentMode == GameSettings.GameMode.ReinforcementLearning)
        {
            if (rlAgent != null && rlAgent.isActiveAndEnabled)
            {
                rlAgent.EndEpisode(); 
            }
        }
        else
        {
            MazeManager.Instance.GenerateNewLevel();
        
            if (MazeManager.Instance.agent != null)
            {
                MazeManager.Instance.agent.ResetAgent(MazeManager.Instance.agent.transform.position);
            }
        }
    }

    public void OnMenuClicked()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}