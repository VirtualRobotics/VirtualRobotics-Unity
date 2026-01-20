using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;

public class GameModeManager : MonoBehaviour
{
    [Header("Referencje do Robota")]
    public GameObject robot;
    public HeuristicMovement heuristicScript;
    public RLAgentController rlScript;
    
    [Header("Inne systemy")]
    public TcpClientController tcpController;
    public CameraStreamer cameraStreamer;

    void Start()
    {
        SetupGameMode();
    }

    void SetupGameMode()
    {
        var mode = GameSettings.CurrentMode;
        Debug.Log($"[GameModeManager] Ustawianie trybu: {mode}");

        if (mode == GameSettings.GameMode.HeuristicCV)
        {
            // --- TRYB 1: Python + OpenCV ---
            // Włączamy:
            if(heuristicScript) heuristicScript.enabled = true;
            if(tcpController) tcpController.enabled = true;
            if(cameraStreamer) cameraStreamer.enabled = true;

            // Wyłączamy:
            if(rlScript) rlScript.enabled = false;
            // Ważne: ML-Agents ma dodatkowe komponenty, które warto wyłączyć
            var decider = robot.GetComponent<DecisionRequester>();
            if (decider) decider.enabled = false;
        }
        else if (mode == GameSettings.GameMode.ReinforcementLearning)
        {
            // --- TRYB 2: Sieć Neuronowa (RL) ---
            // Włączamy:
            if(rlScript) rlScript.enabled = true;
            var decider = robot.GetComponent<DecisionRequester>();
            if (decider) decider.enabled = true;

            // Wyłączamy:
            if(heuristicScript) heuristicScript.enabled = false;
            if(tcpController) tcpController.enabled = false;
            if(cameraStreamer) cameraStreamer.enabled = false;
        }
    }
}