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
    [SerializeField] private Toggle keepMapToggle;
    [SerializeField] private Toggle emptyMazeToggle;

    [Header("Scene")]
    [SerializeField] private string menuSceneName = "MenuScene";

    private void Start()
    {
        RefreshUIFromSettings();
    }

    private void RefreshUIFromSettings()
    {
        if (modeText != null)
            modeText.text = $"MODE: {GameSettings.CurrentMode}";

        if (widthInput != null)
            widthInput.text = GameSettings.MazeWidth.ToString();

        if (heightInput != null)
            heightInput.text = GameSettings.MazeHeight.ToString();

        if (keepMapToggle != null)
            keepMapToggle.isOn = GameSettings.KeepMapLayout;

        if (emptyMazeToggle != null)
            emptyMazeToggle.isOn = GameSettings.GenerateEmptyMaze;
    }

    public void OnResetAndApplyClicked()
    {
        bool sizeChanged = ApplyMazeSizeFromInputs();
        ApplyToggles();

        bool shouldRegenerate = sizeChanged || !GameSettings.KeepMapLayout;

        var mm = MazeManager.Instance;
        if (mm == null)
        {
            Debug.LogWarning("[UI] MazeManager.Instance is null.");
            return;
        }

        if (shouldRegenerate)
        {
            Debug.Log("[UI] Regenerate maze + reset agent.");
            mm.ReloadAndGenerate();
        }
        else
        {
            Debug.Log("[UI] Keep maze layout, reset agent pose only.");
            mm.ResetAgentPositionOnly();
        }

        // opcjonalnie odśwież tekst po zmianach
        if (modeText != null)
            modeText.text = $"MODE: {GameSettings.CurrentMode}";
    }

    private bool ApplyMazeSizeFromInputs()
    {
        bool changed = false;

        if (widthInput != null && int.TryParse(widthInput.text, out int w))
        {
            int newW = Mathf.Max(5, w);
            if (GameSettings.MazeWidth != newW)
            {
                GameSettings.MazeWidth = newW;
                changed = true;
            }
        }

        if (heightInput != null && int.TryParse(heightInput.text, out int h))
        {
            int newH = Mathf.Max(5, h);
            if (GameSettings.MazeHeight != newH)
            {
                GameSettings.MazeHeight = newH;
                changed = true;
            }
        }

        return changed;
    }

    private void ApplyToggles()
    {
        if (keepMapToggle != null)
            GameSettings.KeepMapLayout = keepMapToggle.isOn;

        if (emptyMazeToggle != null)
            GameSettings.GenerateEmptyMaze = emptyMazeToggle.isOn;
    }

    public void OnMenuClicked()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}
