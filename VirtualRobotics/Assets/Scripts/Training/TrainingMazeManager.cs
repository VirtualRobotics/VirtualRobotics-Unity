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
    [SerializeField] private float agentY = 0.2f; // Offset Y, żeby agent nie przenikał przez podłogę

    // Czysty model danych (POCO) - nasza logika biznesowa odcięta od Unity.
    private MazeGenerator _generator;
    
    // Stan (State) lokalnego środowiska - instancje konkretnych obiektów na scenie.
    private GameObject _currentAgent;
    private GameObject _currentGoal;

    /// <summary>
    /// Metoda "Setup", wywoływana jednorazowo przy tworzeniu środowiska przez głównego Siewcę (TrainingEnvManager).
    /// </summary>
    public void Initialize(int w, int h, GameObject agentPrefab)
    {
        // Tworzymy nową instancję logiki matematycznej.
        _generator = new MazeGenerator(w, h);
        
        // Klonujemy (Instantiate) prefaba agenta.
        // Drugi argument (transform) przypina agenta jako dziecko TEGO środowiska (EnvRoot).
        // Dzięki temu agent fizycznie należy do swojej "piaskownicy".
        _currentAgent = Instantiate(agentPrefab, transform);
    }

    /// <summary>
    /// Cykl życia ML-Agents: ta metoda jest wołana na początku każdego nowego epizodu.
    /// Realizuje przepływ (Flow) w sposób imperatywny i czytelny (SRP).
    /// </summary>
    public void RefreshLevel(bool isEmpty)
    {
        Cleanup();                    // 1. Zniszcz stare klocki
        _generator.Generate(isEmpty); // 2. Oblicz nowy rozkład jazdy (macierz)
        ConstructVisuals();           // 3. Zbuduj nowy świat z klocków
        PlaceEntities();              // 4. Rozstaw aktorów (Agent i Cel)
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
                // Przeliczamy abstrakcyjny indeks [x, z] na fizyczne współrzędne w metrach.
                Vector3 pos = CellToWorld(x, z, 0);
                
                // Zawsze kładziemy podłogę. 
                // Quaternion.identity to po prostu brak rotacji (odpowiednik rotacji 0,0,0).
                Instantiate(floorPrefab, pos, Quaternion.identity, worldRoot);
                
                // Jeśli model danych mówi "tu jest ściana (1)":
                if (grid[x, z] == 1)
                {
                    // Ściany w Unity mają swój punkt centralny (pivot) zazwyczaj na środku bryły.
                    // Podnosimy ją o połowę wysokości (cellSize / 2f), żeby nie była w połowie zakopana pod ziemią.
                    Instantiate(wallPrefab, pos + Vector3.up * (cellSize / 2f), Quaternion.identity, worldRoot);
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

        // Zabezpieczenie na wypadek ekstremalnie małego labiryntu
        if (emptyCells.Count < 2) 
        {
            Debug.LogError("Labirynt jest za mały, żeby pomieścić agenta i cel!");
            return;
        }

        // 2. Losujemy pozycję dla Agenta (Random.Range dla int działa WYŁĄCZNIE dla górnej granicy)
        int agentIndex = UnityEngine.Random.Range(0, emptyCells.Count);
        Vector2Int startPoint = emptyCells[agentIndex];
        
        // 3. Usuwamy tę pozycję z listy, żeby Cel na niej nie wylądował
        emptyCells.RemoveAt(agentIndex);

        // 4. Losujemy pozycję dla Celu z POZOSTAŁYCH wolnych miejsc
        int goalIndex = UnityEngine.Random.Range(0, emptyCells.Count);
        Vector2Int goalPoint = emptyCells[goalIndex];

        // 5. Rozstawiamy obiekty na scenie
        _currentAgent.transform.position = CellToWorld(startPoint.x, startPoint.y, agentY);
        
        if (_currentGoal != null) Destroy(_currentGoal);
        _currentGoal = Instantiate(goalPrefab, CellToWorld(goalPoint.x, goalPoint.y, 0.4f), Quaternion.identity, worldRoot);
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

}