using System.Collections.Generic;
using UnityEngine;

public class MazeBuilder : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Transform worldRoot; 
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject goalPrefab;

    [Header("Settings")]
    [SerializeField] private float cellSize = 1f;

    [Header("Spawn Settings")]
    [SerializeField] private float goalSpawnY = 0.4f;
    [SerializeField] private float agentSpawnY = 0.2f;
    
    [Header("Randomization Options")]
    [SerializeField] private bool randomizeSpawnYaw = true;
    [SerializeField] private float spawnYawRange = 180f;
    
    private MazeGenerator _generator;
    
    private GameObject _currentAgent;
    private GameObject _currentGoal;

    public void Initialize(GameObject agentPrefab, int width, int height, bool isEmpty, int seed)
    {
        // 1. Tworzymy agenta, ale OD RAZU go wyłączamy!
        // Dzięki temu silnik fizyczny nie wyrzuci go w kosmos, jeśli zespawni się wewnątrz ściany.
        _currentAgent = Instantiate(agentPrefab, transform);
        _currentAgent.SetActive(false); 
        
        // 2. Budujemy pierwszy poziom
        RefreshLevel(width, height, isEmpty, seed);
    }

    public void RefreshLevel(int width, int height, bool isEmpty, int seed)
    {
        // 1. Ustawiamy seed
        UnityEngine.Random.InitState(seed);
        
        // 2. Usypiamy agenta na czas wyburzania i stawiania ścian, żeby go nie zgniotło
        if (_currentAgent != null) _currentAgent.SetActive(false);

        Cleanup();
    
        // 3. Generujemy nowy labirynt
        _generator = new MazeGenerator(width, height);
        _generator.Generate(isEmpty); 
    
        ConstructVisuals();           
        PlaceEntities();              
    }

    private void ConstructVisuals()
    {
        int[,] grid = _generator.Grid;
        
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int z = 0; z < grid.GetLength(1); z++)
            {
                Vector3 pos = CellToWorld(x, z, 0); 
                GameObject floor = Instantiate(floorPrefab, pos, floorPrefab.transform.rotation, worldRoot); 
                floor.transform.localScale = new Vector3(cellSize, cellSize, 1f);
                
                if (grid[x, z] == 1) 
                {
                    Vector3 wallPos = pos + Vector3.up * (cellSize / 2f);
                    GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity, worldRoot);
                    wall.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                }
            }
        }
    }

    private void PlaceEntities()
    {
        List<Vector2Int> emptyCells = GetEmptyCells();
        
        if (emptyCells.Count < 2) 
        {
            Debug.LogError("Labirynt jest za mały, żeby pomieścić agenta i cel!");
            return;
        }

        // Losowanie pozycji Agenta
        int agentIndex = UnityEngine.Random.Range(0, emptyCells.Count);
        Vector2Int startPoint = emptyCells[agentIndex];
        emptyCells.RemoveAt(agentIndex); 

        // Losowanie pozycji Celu
        int goalIndex = UnityEngine.Random.Range(0, emptyCells.Count);
        Vector2Int goalPoint = emptyCells[goalIndex];
        
        float yaw = randomizeSpawnYaw ? UnityEngine.Random.Range(-spawnYawRange, spawnYawRange) : 0f;
        Quaternion startRotation = Quaternion.Euler(0f, yaw, 0f);

        // --- FIZYCZNE USTAWIENIE AGENTA ---
        if (_currentAgent != null)
        {
            Vector3 targetPos = CellToWorld(startPoint.x, startPoint.y, agentSpawnY);
            
            _currentAgent.transform.position = targetPos;
            _currentAgent.transform.rotation = startRotation;
            
            // Wymuszamy na fizyce przyjęcie nowych koordynatów
            Rigidbody rb = _currentAgent.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = targetPos;
                rb.rotation = startRotation;
                rb.linearVelocity = Vector3.zero; 
                rb.angularVelocity = Vector3.zero;
            }
            
            // Dopiero teraz, gdy Agent jest bezpieczny na pustym polu, WŁĄCZAMY GO!
            _currentAgent.SetActive(true);
        }
        
        // --- USTAWIENIE CELU ---
        if (_currentGoal != null) Destroy(_currentGoal);
        _currentGoal = Instantiate(goalPrefab, CellToWorld(goalPoint.x, goalPoint.y, goalSpawnY), Quaternion.identity, worldRoot);
    }

    private List<Vector2Int> GetEmptyCells()
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        int[,] grid = _generator.Grid;

        for (int x = 1; x < grid.GetLength(0) - 1; x++)
        {
            for (int z = 1; z < grid.GetLength(1) - 1; z++)
            {
                if (grid[x, z] == 0) cells.Add(new Vector2Int(x, z));
            }
        }
        return cells;
    }

    private void Cleanup()
    {
        foreach (Transform child in worldRoot) 
        {
            Destroy(child.gameObject);
        }
    }

    private Vector3 CellToWorld(int x, int z, float y) 
    {
        return transform.position + new Vector3(x * cellSize, y, z * cellSize);
    }
    
    public Transform CurrentGoal => _currentGoal != null ? _currentGoal.transform : null;
}