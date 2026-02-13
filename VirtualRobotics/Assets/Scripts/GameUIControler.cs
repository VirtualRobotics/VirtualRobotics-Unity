using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameUIController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI modeText;
    public string menuSceneName = "MenuScene";
    public TMP_InputField widthInput;
    public TMP_InputField heightInput;

    [Header("Agent References")]
    public RLAgentController rlAgent; 

    void Start()
    {
        UpdateModeText();
        
        if (widthInput) widthInput.text = GameSettings.MazeWidth.ToString();
        if (heightInput) heightInput.text = GameSettings.MazeHeight.ToString();
    }

    void UpdateModeText()
    {
        if (modeText) 
            modeText.text = $"MODE: {GameSettings.CurrentMode}";
    }

    public void OnResetAndApplyClicked()
    {
        Debug.Log("UI: Aktualizacja wymiarów i Reset.");

        bool sizeChanged = false;

        if (widthInput && int.TryParse(widthInput.text, out int w))
        {
            if (GameSettings.MazeWidth != Mathf.Max(5, w))
            {
                GameSettings.MazeWidth = Mathf.Max(5, w);
                sizeChanged = true;
            }
        }

        if (heightInput && int.TryParse(heightInput.text, out int h))
        {
            if (GameSettings.MazeHeight != Mathf.Max(5, h))
            {
                GameSettings.MazeHeight = Mathf.Max(5, h);
                sizeChanged = true;
            }
        }
        
        if (GameSettings.CurrentMode == GameSettings.GameMode.ReinforcementLearning)
        {
            if (MazeManager.Instance != null)
            {
                MazeManager.Instance.ReloadAndGenerate();
            }

            if (rlAgent != null)
            {
                Rigidbody rb = rlAgent.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();
                }

                if (rlAgent.isActiveAndEnabled)
                {
                    rlAgent.EndEpisode(); 
                }
            
                if (rb != null) rb.WakeUp();
            }
        }
        else
        {
            if (MazeManager.Instance != null)
            {
                MazeManager.Instance.ReloadAndGenerate();
            }
            
            if (MazeManager.Instance.agent != null)
            {
                MazeManager.Instance.agent.ResetAgent(new Vector3(1, 0.2f, 1));
            }
        }
    }

    public void OnMenuClicked()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}