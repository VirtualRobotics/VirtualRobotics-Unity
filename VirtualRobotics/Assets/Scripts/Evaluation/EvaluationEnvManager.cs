using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public class EvaluationEnvManager : MonoBehaviour
{
    public static EvaluationEnvManager Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject envRootPrefab; 
    
    [Header("Agents")]
    [SerializeField] private GameObject rlAgentPrefab; 
    [SerializeField] private GameObject cvAgentPrefab; 

    [Header("Scene Controllers (Optional)")]
    [SerializeField] private CameraStreamer cameraStreamer;
    [SerializeField] private TcpClientController tcpController;
    
    private int _currentSeed;

    private MazeBuilder _currentEnvironment;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 1. Setup Scene-level networking and cameras based on mode
        ApplySceneModeSettings();

        // 2. Initialize Seed
        _currentSeed = EvaluationSettings.UseCustomSeed ? EvaluationSettings.StartingSeed : UnityEngine.Random.Range(0, 999999);
        
        // 3. Build the first map
        GenerateNewLevel();
    }

    public void ReloadAndGenerate()
    {
        
        ApplySceneModeSettings();
        
        // Zamiast czytać z Inspektora, czytamy z globalnych ustawień!
        ResetSeedToStartingValue();
        
        GenerateNewLevel();
    }

    public void GenerateNewLevel()
    {
        // 1. Clean up old environment
        if (_currentEnvironment != null)
        {
            Destroy(_currentEnvironment.gameObject);
        }

        // 2. Determine settings from EvaluationEvaluationSettings
        int width = Mathf.Max(EvaluationSettings.MazeWidth % 2 == 0 ? EvaluationSettings.MazeWidth + 1 : EvaluationSettings.MazeWidth, 5);
        int height = Mathf.Max(EvaluationSettings.MazeHeight % 2 == 0 ? EvaluationSettings.MazeHeight + 1 : EvaluationSettings.MazeHeight, 5);
        bool isEmpty = EvaluationSettings.GenerateEmptyMaze;

        // 3. Select the correct agent prefab
        GameObject agentPrefabToUse = SelectAgentPrefab();

        // 4. Spawn the Environment Root (which has MazeBuilder attached)
        GameObject envObj = Instantiate(envRootPrefab, Vector3.zero, Quaternion.identity, transform);
        _currentEnvironment = envObj.GetComponent<MazeBuilder>();

        // 5. Initialize and build the environment with the CURRENT SEED
        _currentEnvironment.Initialize(agentPrefabToUse, width, height, isEmpty, _currentSeed); 

        // 6. Increment seed for the next evaluation run
        _currentSeed++;
    }

    private GameObject SelectAgentPrefab()
    {
        return (EvaluationSettings.CurrentMode == EvaluationSettings.GameMode.HeuristicCV) 
            ? cvAgentPrefab 
            : rlAgentPrefab; 
    }

    private void ApplySceneModeSettings()
    {
        // Auto-resolve if we forgot to drag them in the inspector
        if (cameraStreamer == null) cameraStreamer = FindObjectOfType<CameraStreamer>(true);
        if (tcpController == null) tcpController = FindObjectOfType<TcpClientController>(true);

        bool isCvMode = EvaluationSettings.CurrentMode == EvaluationSettings.GameMode.HeuristicCV;

        if (cameraStreamer != null)
        {
            cameraStreamer.enabled = isCvMode;
            cameraStreamer.enableStreaming = isCvMode;
        }

        if (tcpController != null)
        {
            tcpController.enabled = isCvMode;
        }
        
        Debug.Log($"[EvaluationEnvManager] Scene setup complete for mode: {EvaluationSettings.CurrentMode}");
    }
    
    public void ResetSeedToStartingValue()
    {
        _currentSeed = EvaluationSettings.UseCustomSeed 
            ? EvaluationSettings.StartingSeed 
            : UnityEngine.Random.Range(0, 999999);
    }
    
    private void Update()
    {
        // ML-Agents uwielbia nadpisywać timeScale w trybie Inference.
        // Brutalnie wymuszamy normalny czas (1x) i normalną fizykę.
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}