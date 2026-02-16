using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Potrzebne do obsługi list (Where, ToList)

public class MazeManager : MonoBehaviour
{
    public static MazeManager Instance { get; private set; }

    [Header("Maze Settings")]
    [SerializeField] private int width = 11;
    [SerializeField] private int height = 11;
    [SerializeField] private float cellSize = 1f;

    [Header("Agents Prefabs")]
    [SerializeField] private GameObject inferenceAgentPrefab;
    [SerializeField] private GameObject trainingAgentPrefab;

    [Header("Prefabs")]
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;
    [SerializeField] private GameObject goalPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Vector2Int defaultStartCell = new Vector2Int(1, 1);
    [SerializeField] private float agentSpawnY = 0.2f;
    [SerializeField] private float goalSpawnY = 0.4f;
    
    [Header("Randomization Options")]
    [SerializeField] private bool randomizeSpawns = false;
    [SerializeField] private bool randomizeSpawnYaw = true;
    [SerializeField] private float spawnYawRange = 180f;

    [Header("Generation Options")]
    [SerializeField] private bool generateEmptyMaze = false;

    public int[,] MazeGrid => _maze;

    private int[,] _maze;
    private Transform _goalTf;

    private GameObject _currentAgent;
    private Rigidbody _currentAgentRb;

    private Vector2Int _currentStartCell; 

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
        ApplySettingsFromGameSettings();
        _currentStartCell = defaultStartCell; 
        
        SpawnAgentForCurrentMode();
        GenerateNewLevel();
    }

    public void ReloadAndGenerate()
    {
        ApplySettingsFromGameSettings();
        GenerateNewLevel();
    }

    public void GenerateNewLevel()
    {
        CleanupWorld();
        BuildGrid();
        CarveOrEmptyMaze();
        BuildWorldFromGrid();
        
        // 1. Najpierw ustal gdzie stoi agent (losowo lub default)
        DetermineAgentStartPosition();
        
        // 2. Potem postaw cel (unikając pozycji agenta)
        PlaceGoal();
        
        // 3. Fizycznie przesuń agenta
        ResetAgentPose();
    }

    public void ResetAgentPositionOnly()
    {
        // Jeśli chcemy, aby "Reset Pozycji" również losował nowe miejsca
        // na istniejącej mapie, odkomentuj te dwie linie:
        /*
        if (_goalTf != null) Destroy(_goalTf.gameObject);
        DetermineAgentStartPosition();
        PlaceGoal();
        */

        ResetAgentPose();
    }

    private void ApplySettingsFromGameSettings()
    {
        width = GameSettings.MazeWidth;
        height = GameSettings.MazeHeight;
        generateEmptyMaze = GameSettings.GenerateEmptyMaze;
        
        randomizeSpawns = GameSettings.RandomizeSpawns;

        if (width % 2 == 0) width++;
        if (height % 2 == 0) height++;

        width = Mathf.Max(width, 5);
        height = Mathf.Max(height, 5);
    }

    private void SpawnAgentForCurrentMode()
    {
        DestroyExistingAgent();
        var prefab = SelectAgentPrefab();
        if (prefab == null) return;

        _currentAgent = Instantiate(prefab);
        _currentAgent.tag = "Agent"; 

        _currentAgentRb = _currentAgent.GetComponent<Rigidbody>();
    }

    private GameObject SelectAgentPrefab()
    {
        return (GameSettings.CurrentMode == GameSettings.GameMode.Training) ? trainingAgentPrefab : inferenceAgentPrefab;
    }

    private void DestroyExistingAgent()
    {
        var existing = GameObject.FindGameObjectWithTag("Agent");
        if (existing != null) Destroy(existing);
        _currentAgent = null;
        _currentAgentRb = null;
    }

    private void CleanupWorld()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        _goalTf = null;
    }

    private void BuildGrid()
    {
        _maze = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                _maze[x, z] = 1;
    }

    private void CarveOrEmptyMaze()
    {
        if (generateEmptyMaze)
        {
            FillEmptyWithBorderWalls();
            EnsureCellIsPath(defaultStartCell.x, defaultStartCell.y);
            return;
        }

        ApplyDFS(defaultStartCell.x, defaultStartCell.y);
        EnsureCellIsPath(defaultStartCell.x, defaultStartCell.y);
    }

    private void DetermineAgentStartPosition()
    {
        if (randomizeSpawns)
        {
            // Znajdź losowe wolne pole
            Vector2Int randomPos = GetRandomEmptyCell();
            if (randomPos != new Vector2Int(-1, -1))
            {
                _currentStartCell = randomPos;
                return;
            }
            Debug.LogWarning("Nie znaleziono wolnego miejsca dla agenta, używam default.");
        }

        // Jeśli nie losujemy lub nie znaleziono miejsca
        _currentStartCell = defaultStartCell;
        
        // Zabezpieczenie: jeśli wylosowana mapa ma ścianę na startCell (rzadkie w DFS, ale możliwe w custom)
        EnsureCellIsPath(_currentStartCell.x, _currentStartCell.y);
    }

    private void PlaceGoal()
    {
        Vector2Int goalPos = new Vector2Int(-1, -1);

        if (randomizeSpawns)
        {
            // Losuj cel, ale wyklucz pozycję agenta (_currentStartCell)
            goalPos = GetRandomEmptyCell(excludePos: _currentStartCell);
        }
        else
        {
            // Klasyczne szukanie od końca (z pominięciem agenta)
            goalPos = FindFarCornerGoal(excludePos: _currentStartCell);
        }

        // Jeśli znaleziono poprawne miejsce
        if (goalPos.x != -1)
        {
            Vector3 pos = CellToWorld(goalPos.x, goalPos.y, goalSpawnY);
            _goalTf = Instantiate(goalPrefab, pos, Quaternion.identity, transform).transform;
        }
        else
        {
            Debug.LogWarning("[MazeManager] Could not place goal.");
        }
    }

    private Vector2Int GetRandomEmptyCell(Vector2Int? excludePos = null)
    {
        List<Vector2Int> emptyCells = new List<Vector2Int>();

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < height - 1; z++)
            {
                // 0 to podłoga
                if (_maze[x, z] == 0)
                {
                    Vector2Int pos = new Vector2Int(x, z);
                    // Jeśli mamy wykluczyć konkretne pole (np. tam stoi już agent)
                    if (excludePos.HasValue && pos == excludePos.Value)
                        continue;

                    emptyCells.Add(pos);
                }
            }
        }

        if (emptyCells.Count > 0)
        {
            return emptyCells[Random.Range(0, emptyCells.Count)];
        }

        return new Vector2Int(-1, -1);
    }

    private Vector2Int FindFarCornerGoal(Vector2Int excludePos)
    {
        for (int x = width - 2; x > 0; x--)
        {
            for (int z = height - 2; z > 0; z--)
            {
                if (_maze[x, z] == 0)
                {
                    if (x == excludePos.x && z == excludePos.y) continue;
                    return new Vector2Int(x, z);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    private void ResetAgentPose()
    {
        if (_currentAgent == null) return;

        // Używamy ustalonej wcześniej pozycji _currentStartCell
        Vector3 pos = CellToWorld(_currentStartCell.x, _currentStartCell.y, agentSpawnY);

        float yaw = 0f;
        if (randomizeSpawnYaw)
            yaw = Random.Range(-spawnYawRange, spawnYawRange);

        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        if (_currentAgentRb != null)
        {
            _currentAgentRb.linearVelocity = Vector3.zero; // Unity 6 (w starszych velocity)
            _currentAgentRb.angularVelocity = Vector3.zero;
            _currentAgentRb.position = pos;
            _currentAgentRb.rotation = rot;
            _currentAgentRb.Sleep(); // Ważne przy teleporcie
        }
        else
        {
            _currentAgent.transform.position = pos;
            _currentAgent.transform.rotation = rot;
        }
    }
    
    private void FillEmptyWithBorderWalls()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                bool border = x == 0 || z == 0 || x == width - 1 || z == height - 1;
                _maze[x, z] = border ? 1 : 0;
            }
        }
    }

    private void EnsureCellIsPath(int x, int z)
    {
        if (IsInBounds(x, z)) _maze[x, z] = 0;
    }

    private void BuildWorldFromGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 cellWorld = CellToWorld(x, z, 0f);
                var floor = Instantiate(floorPrefab, cellWorld, Quaternion.identity, transform);
                floor.transform.localScale = new Vector3(cellSize * 0.1f, 1f, cellSize * 0.1f);

                if (_maze[x, z] == 1)
                {
                    Vector3 wallPos = cellWorld + Vector3.up * (cellSize / 2f);
                    var wall = Instantiate(wallPrefab, wallPos, Quaternion.identity, transform);
                    wall.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                }
            }
        }
    }

    private void ApplyDFS(int x, int z)
    {
        _maze[x, z] = 0;
        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            new Vector2Int(x - 2, z), new Vector2Int(x + 2, z),
            new Vector2Int(x, z - 2), new Vector2Int(x, z + 2)
        };
        Shuffle(neighbors);

        foreach (var next in neighbors)
        {
            if (!IsInBounds(next.x, next.y)) continue;
            if (_maze[next.x, next.y] != 1) continue;

            _maze[(x + next.x) / 2, (z + next.y) / 2] = 0;
            ApplyDFS(next.x, next.y);
        }
    }

    private void Shuffle(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    private bool IsInBounds(int x, int z) => x > 0 && x < width - 1 && z > 0 && z < height - 1;
    public Vector3 CellToWorld(int x, int z, float y) => new Vector3(x * cellSize, y, z * cellSize);
}