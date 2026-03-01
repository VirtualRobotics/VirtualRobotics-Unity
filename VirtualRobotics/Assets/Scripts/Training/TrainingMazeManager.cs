using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kontroler środowiska (Orkiestrator). Dziedziczy po MonoBehaviour, 
/// co pozwala mu żyć na scenie Unity i manipulować obiektami 3D.
/// Odpowiada za cykl życia pojedynczego środowiska treningowego.
/// </summary>
public class TrainingMazeManager : MonoBehaviour
{
    [Header("Dependencies")]
    // [SerializeField] działa jak @Autowired / @Inject w Javie (Spring). 
    // Pozwala przypisać referencje prosto z edytora Unity bez upubliczniania zmiennej (hermetyzacja).
    
    // worldRoot to dedykowany kontener (pusty GameObject). 
    // Wrzucamy do niego ściany i podłogi, co drastycznie ułatwia sprzątanie.
    [SerializeField] private Transform worldRoot; 
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject goalPrefab;

    [Header("Settings")]
    [SerializeField] private float cellSize = 1f;

    [Header("Spawn Settings (Matched with Original)")]
    [SerializeField] private float goalSpawnY = 0.4f;
    [SerializeField] private float agentSpawnY = 0.2f;
    
    [Header("Randomization Options")]
    [SerializeField] private bool randomizeSpawnYaw = true;
    [SerializeField] private float spawnYawRange = 180f;

    // Czysty model danych (POCO) - nasza logika biznesowa odcięta od Unity.
    private MazeGenerator _generator;
    
    // Stan (State) lokalnego środowiska - instancje konkretnych obiektów na scenie.
    private GameObject _currentAgent;
    private GameObject _currentGoal;

    /// <summary>
    /// Metoda "Setup", wywoływana jednorazowo przy tworzeniu środowiska przez głównego Siewcę (TrainingEnvManager).
    /// </summary>
    public void Initialize(GameObject agentPrefab)
    {
        // 1. Tworzymy fizyczną instancję agenta w tym środowisku
        _currentAgent = Instantiate(agentPrefab, transform);
    
        // 2. Pobieramy wartości domyślne z "Siewcy" (TrainingEnvManager)
        // To zapewnia, że przycisnięcie "Play" w Unity od razu zbuduje coś sensownego.
        int defaultW = TrainingEnvManager.Instance.MazeWidth;
        int defaultH = TrainingEnvManager.Instance.MazeHeight;
        bool defaultEmpty = TrainingEnvManager.Instance.GenerateEmptyMaze;

        // 3. Budujemy pierwszy poziom
        RefreshLevel(defaultW, defaultH, defaultEmpty);
    }

    /// <summary>
    /// Cykl życia ML-Agents: ta metoda jest wołana na początku każdego nowego epizodu.
    /// Realizuje przepływ (Flow) w sposób imperatywny i czytelny (SRP).
    /// </summary>
    public void RefreshLevel(int width, int height, bool isEmpty)
    {
        Cleanup();
    
        // Budujemy generator na podstawie parametrów dostarczonych przez Agenta
        _generator = new MazeGenerator(width, height);
        _generator.Generate(isEmpty); 
    
        ConstructVisuals();           
        PlaceEntities();              
    }

    /// <summary>
    /// Tłumaczy dane z macierzy int[,] na obiekty 3D (GameObject) w Unity.
    /// </summary>
    private void ConstructVisuals()
    {
        int[,] grid = _generator.Grid;
        
        // Iterujemy po macierzy. W C# GetLength(0) to wymiar X, a GetLength(1) to wymiar Z.
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                Vector3 pos = CellToWorld(x, z, 0); // Przeliczamy abstrakcyjny indeks [x, z] na fizyczne współrzędne w metrach.
                GameObject floor = Instantiate(floorPrefab, pos, floorPrefab.transform.rotation, worldRoot); // Zawsze kładziemy podłogę. 
                floor.transform.localScale = new Vector3(cellSize, cellSize, 1f);
                
                if (grid[x, z] == 1) // Jeśli model danych mówi "tu jest ściana (1)":
                {
                    // Kładziemy ścianę. Podnosimy ją o połowę wysokości, żeby stała na podłodze.
                    Vector3 wallPos = pos + Vector3.up * (cellSize / 2f);
                    GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity, worldRoot);
                    wall.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                }
            }
        }
    }

    /// <summary>
    /// Losuje unikalne pozycje dla Agenta i Celu używając standardowego Unity Random.
    /// </summary>
    private void PlaceEntities()
    {
        // 1. Zbieramy wszystkie koordynaty, które są podłogą (0)
        List<Vector2Int> emptyCells = GetEmptyCells();
        
        if (emptyCells.Count < 2) // Zabezpieczenie na wypadek ekstremalnie małego labiryntu
        {
            Debug.LogError("Labirynt jest za mały, żeby pomieścić agenta i cel!");
            return;
        }

        // 2. Losowanie pozycji Agenta
        int agentIndex = UnityEngine.Random.Range(0, emptyCells.Count);
        Vector2Int startPoint = emptyCells[agentIndex];
        emptyCells.RemoveAt(agentIndex); // Usuwamy tę pozycję z listy, żeby Cel na niej nie wylądował

        // 4. Losujemy pozycję dla Celu z POZOSTAŁYCH wolnych miejsc
        int goalIndex = UnityEngine.Random.Range(0, emptyCells.Count);
        Vector2Int goalPoint = emptyCells[goalIndex];
        
        // 4. Obliczanie losowej rotacji (Yaw)
        float yaw = 0f;
        if (randomizeSpawnYaw)
        {
            yaw = UnityEngine.Random.Range(-spawnYawRange, spawnYawRange);
        }
        Quaternion startRotation = Quaternion.Euler(0f, yaw, 0f);

        // 5. Fizyczne ustawienie Agenta
        _currentAgent.transform.position = CellToWorld(startPoint.x, startPoint.y, agentSpawnY);
        _currentAgent.transform.rotation = startRotation;
        
        // Zatrzymanie sił fizycznych (zgodnie z oryginalnym _currentAgentRb.Sleep())
        Rigidbody rb = _currentAgent.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; 
            rb.angularVelocity = Vector3.zero;
        }
        
        // 6. Fizyczne ustawienie Celu z użyciem oryginalnego goalSpawnY
        if (_currentGoal != null) Destroy(_currentGoal);
        _currentGoal = Instantiate(goalPrefab, CellToWorld(goalPoint.x, goalPoint.y, goalSpawnY), Quaternion.identity, worldRoot);
    }

    /// <summary>
    /// Skanuje macierz i zwraca listę pustych miejsc (pomijając zewnętrzne ściany).
    /// </summary>
    private List<Vector2Int> GetEmptyCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        int[,] grid = _generator.Grid;

        for (int x = 1; x < grid.GetLength(0) - 1; x++)
        {
            for (int z = 1; z < grid.GetLength(1) - 1; z++)
            {
                if (grid[x, z] == 0) // Jeśli to podłoga
                {
                    cells.Add(new Vector2Int(x, z));
                }
            }
        }
        return cells;
    }

    /// <summary>
    /// Sprzątaczka. Zapobiega wyciekom pamięci i nakładaniu się ścian z poprzednich epizodów.
    /// </summary>
    private void Cleanup()
    {
        // Pętla po wszystkich dzieciach obiektu worldRoot.
        // C# i Unity pozwalają na foreach bezpośrednio po obiekcie Transform.
        foreach (Transform child in worldRoot) 
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// NAJWAŻNIEJSZA METODA DLA MULTI-ENV!
    /// Transformuje współrzędne lokalne siatki labiryntu na współrzędne globalne Unity.
    /// </summary>
    private Vector3 CellToWorld(int x, int z, float y) 
    {
        // "transform.position" to pozycja naszego EnvRoot na głównej scenie.
        // Dodając ją do wektora, sprawiamy, że labirynt buduje się RELATYWNIE do swojego kontenera.
        // Bez tego wszystkie 8 labiryntów zbudowałoby się w jednym punkcie (0, 0, 0) nałożone na siebie!
        return transform.position + new Vector3(x * cellSize, y, z * cellSize);
    }
    
    // Publiczny dostępnik, żeby Agent wiedział, gdzie jest meta w JEGO środowisku
    public Transform CurrentGoal => _currentGoal != null ? _currentGoal.transform : null;

}