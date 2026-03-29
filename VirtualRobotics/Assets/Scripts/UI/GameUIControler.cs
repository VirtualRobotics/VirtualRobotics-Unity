using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI modeText;
    [SerializeField] private TMP_InputField widthInput;
    [SerializeField] private TMP_InputField heightInput;
    [SerializeField] private Toggle emptyMazeToggle;
    
    [Header("Seed UI References")]
    [SerializeField] private Toggle customSeedToggle;
    [SerializeField] private TMP_InputField seedInput;

    [Header("Scene")]
    [SerializeField] private string menuSceneName = "MenuScene";

    private void Start()
    {
        RefreshUIFromSettings();
    }

    private void RefreshUIFromSettings()
    {
        if (modeText != null) modeText.text = $"MODE: {EvaluationSettings.CurrentMode}";
        if (widthInput != null) widthInput.text = EvaluationSettings.MazeWidth.ToString();
        if (heightInput != null) heightInput.text = EvaluationSettings.MazeHeight.ToString();
        if (emptyMazeToggle != null) emptyMazeToggle.isOn = EvaluationSettings.GenerateEmptyMaze;
        
        // Odświeżanie UI Seeda
        if (customSeedToggle != null) customSeedToggle.isOn = EvaluationSettings.UseCustomSeed;
        if (seedInput != null) seedInput.text = EvaluationSettings.StartingSeed.ToString();
    }

    public void OnResetAndApplyClicked()
    {
        ApplyMazeSizeFromInputs();
        ApplySeedFromInputs(); // Zapisz dane seeda
        
        if (emptyMazeToggle != null)
            EvaluationSettings.GenerateEmptyMaze = emptyMazeToggle.isOn;

        if (EvaluationEnvManager.Instance != null)
        {
            Debug.Log("[UI] Settings applied. Resetting seed sequence and generating new level.");
            
            // Ponieważ zmieniamy ustawienia, chcemy zacząć sekwencję seedów od nowa!
            EvaluationEnvManager.Instance.ResetSeedToStartingValue();
            EvaluationEnvManager.Instance.GenerateNewLevel(); 
        }

        if (modeText != null) modeText.text = $"MODE: {EvaluationSettings.CurrentMode}";
    }

    private void ApplyMazeSizeFromInputs()
    {
        if (widthInput != null && int.TryParse(widthInput.text, out int w))
        {
            EvaluationSettings.MazeWidth = Mathf.Max(5, w);
            if (EvaluationSettings.MazeWidth % 2 == 0) EvaluationSettings.MazeWidth++;
        }

        if (heightInput != null && int.TryParse(heightInput.text, out int h))
        {
            EvaluationSettings.MazeHeight = Mathf.Max(5, h);
            if (EvaluationSettings.MazeHeight % 2 == 0) EvaluationSettings.MazeHeight++;
        }
    }

    private void ApplySeedFromInputs()
    {
        if (customSeedToggle != null)
            EvaluationSettings.UseCustomSeed = customSeedToggle.isOn;

        if (seedInput != null && int.TryParse(seedInput.text, out int s))
            EvaluationSettings.StartingSeed = s;
    }

    public void OnMenuClicked()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}