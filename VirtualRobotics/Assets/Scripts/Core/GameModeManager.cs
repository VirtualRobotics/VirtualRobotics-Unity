using UnityEngine;
using Unity.MLAgents;

public class GameModeManager : MonoBehaviour
{
    [Header("Optional References")]
    [SerializeField] private CameraStreamer cameraStreamer;

    [Header("Runtime-resolved")]
    [SerializeField] private GameObject robot;

    // Controllers on the robot:
    [SerializeField] private MonoBehaviour heuristicController;  // CvHeuristicController OR HeuristicMovement (fallback)
    [SerializeField] private RLAgentController rlController;
    [SerializeField] private DecisionRequester decisionRequester;

    // External controller (usually in scene):
    [SerializeField] private TcpClientController tcpController;

    private void Start()
    {
        SetupGameMode();
    }

    public void SetupGameMode()
    {
        ResolveReferences();

        if (cameraStreamer != null)
            cameraStreamer.enabled = true;

        switch (GameSettings.CurrentMode)
        {
            case GameSettings.GameMode.HeuristicCV:
                ApplyHeuristicCvMode();
                break;

            case GameSettings.GameMode.ReinforcementLearning:
                ApplyInferenceRlMode();
                break;

            case GameSettings.GameMode.Training:
                ApplyTrainingMode();
                break;

            default:
                Debug.LogWarning("[GameModeManager] Unknown mode: " + GameSettings.CurrentMode);
                ApplySafeDefaults();
                break;
        }

        Debug.Log($"[GameModeManager] Mode applied: {GameSettings.CurrentMode}. Robot={(robot ? robot.name : "null")}");
    }

    public void ForceSetup()
    {
        SetupGameMode();
    }

    // =========================
    // Mode application
    // =========================

    private void ApplyHeuristicCvMode()
    {
        if (cameraStreamer != null) cameraStreamer.enableStreaming = true;

        SetEnabled(heuristicController, true);
        SetEnabled(tcpController, true);

        SetEnabled(rlController, false);
        SetEnabled(decisionRequester, false);
    }

    private void ApplyInferenceRlMode()
    {
        if (cameraStreamer != null) cameraStreamer.enableStreaming = false;

        SetEnabled(rlController, true);
        SetEnabled(decisionRequester, true);

        SetEnabled(heuristicController, false);
        SetEnabled(tcpController, false);
    }

    private void ApplyTrainingMode()
    {
        if (cameraStreamer != null) cameraStreamer.enabled = false;

        SetEnabled(rlController, true);
        SetEnabled(decisionRequester, true);

        SetEnabled(heuristicController, false);
        SetEnabled(tcpController, false);
    }

    private void ApplySafeDefaults()
    {
        if (cameraStreamer != null) cameraStreamer.enableStreaming = false;

        SetEnabled(heuristicController, false);
        SetEnabled(tcpController, false);
        SetEnabled(rlController, false);
        SetEnabled(decisionRequester, false);
    }

    // =========================
    // Reference resolving
    // =========================

    private void ResolveReferences()
    {
        if (robot == null)
            robot = GameObject.FindGameObjectWithTag("Agent");

        if (robot != null)
        {
            if (heuristicController == null)
            {
                var cv = robot.GetComponent("CvHeuristicController");
                if (cv != null) heuristicController = (MonoBehaviour)cv;
            }

            if (rlController == null)
                rlController = robot.GetComponent<RLAgentController>();

            if (decisionRequester == null)
                decisionRequester = robot.GetComponent<DecisionRequester>();
        }
        else
        {
            heuristicController = null;
            rlController = null;
            decisionRequester = null;
        }

        if (tcpController == null)
            tcpController = FindObjectOfType<TcpClientController>(true);

        if (cameraStreamer == null)
            cameraStreamer = FindObjectOfType<CameraStreamer>(true);
    }

    private void SetEnabled(Object component, bool enabled)
    {
        if (component == null) return;
        if (component is Behaviour b) b.enabled = enabled;
    }
}
