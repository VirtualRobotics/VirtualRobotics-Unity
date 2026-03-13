using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private GameObject infoPanel;
    
    [Header("Labirynth Settings UI")]
    [SerializeField] private GameObject configPanel;
    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;
    [SerializeField] private TMP_InputField seedInput; // Zmienione z toggle!
    [SerializeField] private Toggle emptyMazeToggle;
    
    private void Start()
    {
        if (configPanel != null) configPanel.SetActive(false);
        ResetInputs();
    }

    private void ResetInputs()
    {
        if (widthInput != null) widthInput.text = EvaluationSettings.MazeWidth.ToString();
        if (heightInput != null) heightInput.text = EvaluationSettings.MazeHeight.ToString();
        
        // Jeśli używamy custom seeda, wpisz go. Jeśli nie, zostaw puste pole.
        if (seedInput != null)
        {
            seedInput.text = EvaluationSettings.UseCustomSeed ? EvaluationSettings.StartingSeed.ToString() : "";
        }
    }
    
    public void ToggleInfoPanel(bool show)
    {
        if (infoPanel != null) infoPanel.SetActive(show);
    }

    public void SelectHeuristicMode() => OpenConfig(EvaluationSettings.GameMode.HeuristicCV);
    public void SelectRLMode() => OpenConfig(EvaluationSettings.GameMode.ReinforcementLearning);
    
    private void OpenConfig(EvaluationSettings.GameMode mode)
    {
        EvaluationSettings.CurrentMode = mode;
        
        if (configPanel != null) 
        {
            configPanel.SetActive(true);
            ResetInputs();
            if (emptyMazeToggle != null) emptyMazeToggle.isOn = EvaluationSettings.GenerateEmptyMaze;
        }
        else
        {
            LoadGameScene();
        }
    }
    
    public void OnStartGameClicked()
    {
        SaveSettingsFromInputs();
        
        if (emptyMazeToggle != null) 
            EvaluationSettings.GenerateEmptyMaze = emptyMazeToggle.isOn;

        LoadGameScene();
    }
    
    public void CloseConfigPanel()
    {
        if (configPanel != null) configPanel.SetActive(false);
    }
    
    private void SaveSettingsFromInputs()
    {
        // 1. Zapis wymiarów
        if (widthInput != null && int.TryParse(widthInput.text, out int w))
            EvaluationSettings.MazeWidth = Mathf.Max(5, w); 

        if (heightInput != null && int.TryParse(heightInput.text, out int h))
            EvaluationSettings.MazeHeight = Mathf.Max(5, h);
            
        if (EvaluationSettings.MazeWidth % 2 == 0) EvaluationSettings.MazeWidth++;
        if (EvaluationSettings.MazeHeight % 2 == 0) EvaluationSettings.MazeHeight++;

        // 2. MAGIA SEEDA: Jeśli pole nie jest puste i ma liczbę -> użyj jej. W przeciwnym razie losuj.
        if (seedInput != null && !string.IsNullOrWhiteSpace(seedInput.text) && int.TryParse(seedInput.text, out int s))
        {
            EvaluationSettings.UseCustomSeed = true;
            EvaluationSettings.StartingSeed = s;
        }
        else
        {
            EvaluationSettings.UseCustomSeed = false;
        }
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}