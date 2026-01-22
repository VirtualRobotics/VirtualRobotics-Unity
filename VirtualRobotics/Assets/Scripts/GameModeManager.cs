using UnityEngine;
using Unity.MLAgents;

public class GameModeManager : MonoBehaviour
{
    [Header("References")]
    public CameraStreamer cameraStreamer;
    
    public GameObject robot;
    public HeuristicMovement heuristicScript;
    public RLAgentController rlScript;
    public TcpClientController tcpController;

    void Start()
    {
        SetupGameMode();
    }

    void SetupGameMode()
    {
        var mode = GameSettings.CurrentMode;

        if (cameraStreamer != null) cameraStreamer.enabled = true;

        if (mode == GameSettings.GameMode.HeuristicCV)
        {
            // --- HEURISTIC MODE ---
            if (cameraStreamer) cameraStreamer.enableStreaming = true; 
            
            if(heuristicScript) heuristicScript.enabled = true;
            if(tcpController) tcpController.enabled = true;
            if(rlScript) rlScript.enabled = false;
            
            var decider = robot.GetComponent<DecisionRequester>();
            if (decider) decider.enabled = false;
        }
        else if (mode == GameSettings.GameMode.ReinforcementLearning)
        {
            // --- RL MODE ---
            if (cameraStreamer) cameraStreamer.enableStreaming = false;

            if(rlScript) rlScript.enabled = true;
            var decider = robot.GetComponent<DecisionRequester>();
            if (decider) decider.enabled = true;

            if(heuristicScript) heuristicScript.enabled = false;
            if(tcpController) tcpController.enabled = false;
        }
    }
}