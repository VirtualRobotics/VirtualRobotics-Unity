using UnityEngine;
using System.Collections.Generic;
using System.Linq; 
using System.IO; // DO LOGÓW

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
    
    // ZMIANA: Trzymamy aktywny seed dla loggera
    public int ActiveSeed { get; private set; }

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
        ApplySceneModeSettings();
        _currentSeed = EvaluationSettings.UseCustomSeed ? EvaluationSettings.StartingSeed : UnityEngine.Random.Range(0, 999999);
        GenerateNewLevel();
    }

    public void ReloadAndGenerate()
    {
        ApplySceneModeSettings();
        ResetSeedToStartingValue();
        GenerateNewLevel();
    }

    public void GenerateNewLevel()
    {
        if (_currentEnvironment != null)
        {
            Destroy(_currentEnvironment.gameObject);
        }

        int width = Mathf.Max(EvaluationSettings.MazeWidth % 2 == 0 ? EvaluationSettings.MazeWidth + 1 : EvaluationSettings.MazeWidth, 5);
        int height = Mathf.Max(EvaluationSettings.MazeHeight % 2 == 0 ? EvaluationSettings.MazeHeight + 1 : EvaluationSettings.MazeHeight, 5);
        bool isEmpty = EvaluationSettings.GenerateEmptyMaze;

        GameObject agentPrefabToUse = SelectAgentPrefab();

        GameObject envObj = Instantiate(envRootPrefab, Vector3.zero, Quaternion.identity, transform);
        _currentEnvironment = envObj.GetComponent<MazeBuilder>();

        // Zapisujemy aktywny seed ZANIM go podbijemy (żeby logger użył poprawnego)
        ActiveSeed = _currentSeed;

        _currentEnvironment.Initialize(agentPrefabToUse, width, height, isEmpty, ActiveSeed); 

        _currentSeed++;
    }

    // ==============================================
    // NOWOŚĆ: METODA LOGUJĄCA WYNIKI DO PLIKU CSV
    // ==============================================
    public void LogResult(int steps, bool success)
    {
        // Zapisze plik obok folderu Assets (żeby Unity nie freezowało się przy re-imporcie co klatkę)
        string filePath = Path.Combine(Application.dataPath, "../EvaluationLogs.csv");

        // Jeśli plik nie istnieje, tworzymy nagłówki
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Seed,Mode,Width,Height,EmptyMaze,Steps,Success\n");
        }

        string mode = EvaluationSettings.CurrentMode.ToString();
        int w = EvaluationSettings.MazeWidth;
        int h = EvaluationSettings.MazeHeight;
        bool empty = EvaluationSettings.GenerateEmptyMaze;

        // Składamy dane i dopisujemy na koniec pliku
        string logLine = $"{ActiveSeed},{mode},{w},{h},{empty},{steps},{success}\n";
        File.AppendAllText(filePath, logLine);
        
        Debug.Log($"[Logger] Zapisano: {logLine.Trim()}");
    }

    private GameObject SelectAgentPrefab()
    {
        return (EvaluationSettings.CurrentMode == EvaluationSettings.GameMode.HeuristicCV) 
            ? cvAgentPrefab 
            : rlAgentPrefab; 
    }

    private void ApplySceneModeSettings()
    {
        if (cameraStreamer == null) cameraStreamer = FindFirstObjectByType<CameraStreamer>(FindObjectsInactive.Include);
        if (tcpController == null) tcpController = FindFirstObjectByType<TcpClientController>(FindObjectsInactive.Include);

        bool isCvMode = EvaluationSettings.CurrentMode == EvaluationSettings.GameMode.HeuristicCV;

        if (cameraStreamer != null)
        {
            cameraStreamer.enabled = isCvMode;
            cameraStreamer.enableStreaming = isCvMode;
        }

        if (tcpController != null) tcpController.enabled = isCvMode;
        
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
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}