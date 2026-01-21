using UnityEngine;
using Unity.MLAgents;

public class GameModeManager : MonoBehaviour
{
    [Header("Referencje")]
    public CameraStreamer cameraStreamer; // <--- Używamy Twojej nazwy
    
    public GameObject robot;
    public HeuristicMovement heuristicScript;
    public RLAgentController rlScript;
    public TcpClientController tcpController;
    
    // Te pola możesz usunąć, bo CameraStreamer sam to obsłuży:
    // public Camera agentCamera; 
    // public RenderTexture mazeViewTexture;

    void Start()
    {
        SetupGameMode();
    }

    void SetupGameMode()
    {
        var mode = GameSettings.CurrentMode;

        // Upewnij się, że CameraStreamer jest WŁĄCZONY jako komponent,
        // bo on musi renderować obraz dla UI w obu trybach.
        if (cameraStreamer != null) cameraStreamer.enabled = true;

        if (mode == GameSettings.GameMode.HeuristicCV)
        {
            // --- TRYB HEURYSTYCZNY ---
            // Włączamy wysyłanie danych do Pythona
            if (cameraStreamer) cameraStreamer.enableStreaming = true; 
            
            if(heuristicScript) heuristicScript.enabled = true;
            if(tcpController) tcpController.enabled = true;
            if(rlScript) rlScript.enabled = false;
            
            var decider = robot.GetComponent<DecisionRequester>();
            if (decider) decider.enabled = false;
        }
        else if (mode == GameSettings.GameMode.ReinforcementLearning)
        {
            // --- TRYB RL ---
            // Wyłączamy wysyłanie danych (optymalizacja), ale UI nadal działa
            if (cameraStreamer) cameraStreamer.enableStreaming = false;

            if(rlScript) rlScript.enabled = true;
            var decider = robot.GetComponent<DecisionRequester>();
            if (decider) decider.enabled = true;

            if(heuristicScript) heuristicScript.enabled = false;
            if(tcpController) tcpController.enabled = false;
        }
    }
}