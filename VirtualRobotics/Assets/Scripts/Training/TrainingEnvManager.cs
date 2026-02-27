using UnityEngine;

/// <summary>
/// Siewca (Factory). Jego jedynym zadaniem jest rozstawienie N kopii 
/// naszego prefaba EnvRoot (z TrainingMazeManagerem) w regularnej siatce.
/// </summary>
public class TrainingEnvManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject envRootPrefab; 
    [SerializeField] private GameObject agentPrefab;   

    [Header("Grid Setup")]
    [SerializeField] private int columns = 4;
    [SerializeField] private int rows = 4; // 4x4 = 16 środowisk
    [SerializeField] private float spacing = 20f; // Bezpieczny odstęp w metrach

    [Header("Maze Dimensions (External)")]
    [SerializeField] private int mazeWidth = 3;  // Zewnętrzne 3 da wewnątrz korytarz 1
    [SerializeField] private int mazeHeight = 5; // Zewnętrzne 5 da wewnątrz korytarz 3

    private void Start()
    {
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
            TrainingMazeManager envManager = envObj.GetComponent<TrainingMazeManager>();
            
            // Wstrzykujemy zależności. Agent wylosuje sobie miejsce za pomocą naszej nowej funkcji.
            envManager.Initialize(mazeWidth, mazeHeight, agentPrefab);
            
            // Generujemy labirynt - podajemy true, jeśli chcemy puste "pudło" (np. korytarz 1x3)
            envManager.RefreshLevel(isEmpty: true); 
        }
    }
}