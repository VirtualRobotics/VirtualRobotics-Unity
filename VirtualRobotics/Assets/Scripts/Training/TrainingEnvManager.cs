using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// Siewca (Factory). Jego jedynym zadaniem jest rozstawienie N kopii 
/// naszego prefaba EnvRoot (z TrainingMazeManagerem) w regularnej siatce.
/// </summary>
public class TrainingEnvManager : MonoBehaviour
{
    public static TrainingEnvManager Instance { get; private set; }
    
    [Header("Prefabs")]
    [SerializeField] private GameObject envRootPrefab; 
    [SerializeField] private GameObject agentPrefab;   

    [Header("Grid Setup")]
    [SerializeField] private int columns = 4;
    [SerializeField] private int rows = 4; // 4x4 = 16 środowisk
    [SerializeField] private float spacing = 20f; // Bezpieczny odstęp w metrach


    [Header("Global Environment Settings")]
    public int MazeWidth = 3;  
    public int MazeHeight = 5; 
    public bool GenerateEmptyMaze = true;
    
    private int seed = UnityEngine.Random.Range(0, 999999);
    private void Awake()
    {
        // Ustawiamy Singletona, zanim ktokolwiek go zawoła
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        // Wymuszenie normalnego czasu dla testów (nadpisuje zachowanie ML-Agents)
        Time.timeScale = 1f; 
        Time.fixedDeltaTime = 0.02f; // Domyślny timestep fizyki w Unity (50 klatek fizycznych na sekundę)
        
        SpawnEnvironments();
    }

    private void SpawnEnvironments()
    {
        int totalEnvs = columns * rows;

        for (int i = 0; i < totalEnvs; i++)
        {
            // Przeliczanie 1D na 2D (wiersze i kolumny)
            int row = i / columns;
            int col = i % columns;

            // Ustawienie każdego środowiska w odstępach
            Vector3 spawnPos = new Vector3(col * spacing, 0, row * spacing);
            
            // Klonujemy środowisko i umieszczamy je w hierarchii jako dziecko "Siewcy"
            GameObject envObj = Instantiate(envRootPrefab, spawnPos, Quaternion.identity, transform);
            envObj.name = $"TrainingEnvironment_{row}_{col}";

            // Odpalamy Orkiestratora na sklonowanym środowisku
            MazeBuilder envBuilder = envObj.GetComponent<MazeBuilder>();
            
             // Inicjalizujemy agenta (tutaj agent pojawia się na scenie)
             envBuilder.Initialize(agentPrefab, MazeWidth, MazeHeight, GenerateEmptyMaze, seed);
            
        }
    }
}