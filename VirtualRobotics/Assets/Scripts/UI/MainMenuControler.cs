using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Configuration")]
    public string gameSceneName = "GameScene";
    public GameObject infoPanel;
    
    [Header("Labirynth Settings UI")]
    public GameObject configPanel;
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;
    public Toggle emptyMazeToggle;
    
    void Start()
    {
        if (configPanel) configPanel.SetActive(false);
        ResetInputs();
    }

    void ResetInputs()
    {
        if (widthInput) widthInput.text = GameSettings.MazeWidth.ToString();
        if (heightInput) heightInput.text = GameSettings.MazeHeight.ToString();
    }
    
    public void ToggleInfoPanel(bool show)
    {
        infoPanel.SetActive(show);
    }

    public void SelectHeuristicMode()
    {
        OpenConfig(GameSettings.GameMode.HeuristicCV);
    }

    public void SelectRLMode()
    {
        OpenConfig(GameSettings.GameMode.ReinforcementLearning);
    }

    public void SelectTrainingMode()
    {
        OpenConfig(GameSettings.GameMode.Training);
    }
    
    public void SelectManualMode()
    {
        OpenConfig(GameSettings.GameMode.Manual);
    }
    
    private void OpenConfig(GameSettings.GameMode mode)
    {
        GameSettings.CurrentMode = mode;
        
        if (configPanel) 
        {
            configPanel.SetActive(true);
            ResetInputs();
            
            if (emptyMazeToggle != null)
            {
                emptyMazeToggle.isOn = GameSettings.GenerateEmptyMaze;
            }
        }
        else
        {
            LoadGameScene();
        }
    }
    
    public void OnStartGameClicked()
    {
        SaveSizeFromInputs();
        
        if (emptyMazeToggle != null)
        {
            GameSettings.GenerateEmptyMaze = emptyMazeToggle.isOn;
            Debug.Log($"Empty maze mode: {GameSettings.GenerateEmptyMaze}");
        }

        LoadGameScene();
    }
    
    public void CloseConfigPanel()
    {
        if (configPanel) configPanel.SetActive(false);
    }
    
    private void SaveSizeFromInputs()
    {
        if (widthInput && int.TryParse(widthInput.text, out int w))
            GameSettings.MazeWidth = Mathf.Max(5, w); // Min 5x5

        if (heightInput && int.TryParse(heightInput.text, out int h))
            GameSettings.MazeHeight = Mathf.Max(5, h);
            
        if (GameSettings.MazeWidth % 2 == 0) GameSettings.MazeWidth++;
        if (GameSettings.MazeHeight % 2 == 0) GameSettings.MazeHeight++;
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }
    
}