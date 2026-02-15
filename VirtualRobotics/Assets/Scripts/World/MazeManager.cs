using UnityEngine;
using System.Collections.Generic;

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

    [Header("Spawn")]
    [SerializeField] private Vector2Int startCell = new Vector2Int(1, 1);
    [SerializeField] private float agentSpawnY = 0.2f;
    [SerializeField] private float goalSpawnY = 0.4f;

    [Header("Generation Options")]
    [SerializeField] private bool generateEmptyMaze = false;

    public int[,] MazeGrid => _maze; // jeśli kiedyś będziesz chciał BFS/debug

    private int[,] _maze;
    private Transform _goalTf;

    private GameObject _currentAgent;
    private Rigidbody _currentAgentRb;

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
        SpawnAgentForCurrentMode();
        GenerateNewLevel();
    }

    // ============================
    // Public API
    // ============================
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
        PlaceGoal();
        ResetAgentPose();
    }

    public void ResetAgentPositionOnly()
    {
        ResetAgentPose();
    }

    // ============================
    // Orchestration helpers
    // ============================
    private void ApplySettingsFromGameSettings()
    {
        width = GameSettings.MazeWidth;
        height = GameSettings.MazeHeight;
        generateEmptyMaze = GameSettings.GenerateEmptyMaze;

        // enforce odd sizes (DFS expects walls between cells)
        if (width % 2 == 0) width++;
        if (height % 2 == 0) height++;

        // safety
        width = Mathf.Max(width, 5);
        height = Mathf.Max(height, 5);
    }

    private void SpawnAgentForCurrentMode()
    {
        DestroyExistingAgent();

        var prefab = SelectAgentPrefab();
        if (prefab == null)
        {
            Debug.LogError("[MazeManager] Agent prefab is null.");
            return;
        }

        _currentAgent = Instantiate(prefab);
        _currentAgent.tag = "Agent"; // upewnij się, że tag jest ustawiony

        _currentAgentRb = _currentAgent.GetComponent<Rigidbody>();
        if (_currentAgentRb == null)
        {
            Debug.LogWarning("[MazeManager] Spawned agent has no Rigidbody.");
        }

        // NOTE: Nie wołamy tu GameModeManager.ForceSetup().
        // To GameModeManager powinien sam ogarnąć w Start() na podstawie GameSettings.
    }

    private GameObject SelectAgentPrefab()
    {
        if (GameSettings.CurrentMode == GameSettings.GameMode.Training)
            return trainingAgentPrefab;

        return inferenceAgentPrefab;
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
        // usuń wszystko z MazeManager (oprócz agenta, jeśli jest dzieckiem - ale tu agent nie jest childem)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        _goalTf = null;
    }

    private void BuildGrid()
    {
        _maze = new int[width, height];
        // default: ściany
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                _maze[x, z] = 1;
    }

    private void CarveOrEmptyMaze()
    {
        if (generateEmptyMaze)
        {
            FillEmptyWithBorderWalls();
            EnsureStartCellIsPath();
            return;
        }

        ApplyDFS(startCell.x, startCell.y);
        EnsureStartCellIsPath();
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

    private void EnsureStartCellIsPath()
    {
        if (IsInBounds(startCell.x, startCell.y))
            _maze[startCell.x, startCell.y] = 0;
    }

    private void BuildWorldFromGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 cellWorld = CellToWorld(x, z, 0f);

                // floor
                var floor = Instantiate(floorPrefab, cellWorld, Quaternion.identity, transform);

                // UWAGA: nie ruszam Twojej skali (0.1f) bo mówisz, że to celowe.
                // Ale to jest miejsce, gdzie to powinno być kontrolowane.
                floor.transform.localScale = new Vector3(cellSize * 0.1f, 1f, cellSize * 0.1f);

                // wall
                if (_maze[x, z] == 1)
                {
                    Vector3 wallPos = cellWorld + Vector3.up * (cellSize / 2f);
                    var wall = Instantiate(wallPrefab, wallPos, Quaternion.identity, transform);
                    wall.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                }
            }
        }
    }

    private void PlaceGoal()
    {
        // znajdź pierwszy path od końca
        for (int x = width - 2; x > 0; x--)
        {
            for (int z = height - 2; z > 0; z--)
            {
                if (_maze[x, z] == 0)
                {
                    Vector3 pos = CellToWorld(x, z, goalSpawnY);
                    _goalTf = Instantiate(goalPrefab, pos, Quaternion.identity, transform).transform;
                    return;
                }
            }
        }

        Debug.LogWarning("[MazeManager] Could not place goal (no path found).");
    }

    private void ResetAgentPose()
    {
        if (_currentAgent == null)
        {
            Debug.LogWarning("[MazeManager] No current agent to reset.");
            return;
        }

        Vector3 pos = CellToWorld(startCell.x, startCell.y, agentSpawnY);

        if (_currentAgentRb != null)
        {
            _currentAgentRb.linearVelocity = Vector3.zero;
            _currentAgentRb.angularVelocity = Vector3.zero;

            _currentAgentRb.position = pos;
            _currentAgentRb.rotation = Quaternion.identity; // możesz tu dać losowy yaw
        }
        else
        {
            _currentAgent.transform.position = pos;
            _currentAgent.transform.rotation = Quaternion.identity;
        }
    }

    // ============================
    // DFS generation
    // ============================
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

    private bool IsInBounds(int x, int z)
        => x > 0 && x < width - 1 && z > 0 && z < height - 1;

    // ============================
    // Grid/world helpers
    // ============================
    public Vector3 CellToWorld(int x, int z, float y)
        => new Vector3(x * cellSize, y, z * cellSize);
}
