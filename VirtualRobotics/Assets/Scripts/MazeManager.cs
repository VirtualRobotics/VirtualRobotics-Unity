using UnityEngine;
using System.Collections.Generic;

public class MazeManager : MonoBehaviour
{
    public static MazeManager Instance;

    [Header("Maze Settings")]
    public int width = 11;
    public int height = 11;
    public float cellSize = 1f;

    [Header("Prefabs and References")]
    public HeuristicMovement agent;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject goalPrefab;

    private int[,] _maze;

    void Awake() => Instance = this;

    void Start() => GenerateNewLevel();

    public void GenerateNewLevel()
    {
        // 1. Clean up old objects
        foreach (Transform child in transform) {
            if (child.gameObject.CompareTag("Agent")) continue;
            Destroy(child.gameObject);
        }

        // 2. Initialize array
        _maze = new int[width, height];
        for (int x = 0; x < width; x++) {
            for (int z = 0; z < height; z++) {
                _maze[x, z] = 1;
            }
        }

        // 3. DFS Algorithm - carves tunnels (0) in walls (1)
        ApplyDFS(1, 1);

        // 4. Physical construction
        for (int x = 0; x < width; x++) {
            for (int z = 0; z < height; z++) {
                Vector3 pos = new Vector3(x * cellSize, 0, z * cellSize);
                
                GameObject floor = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                floor.transform.localScale = new Vector3(cellSize * 0.1f, 1f, cellSize * 0.1f);

                if (_maze[x, z] == 1) {
                    // WALL (Prefab 1x1x1)
                    Vector3 wallPos = pos + Vector3.up * (cellSize / 2f);
                    GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity, transform);
                    wall.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                }
            }
        }

        // 5. Place goal and reset agent
        PlaceGoal();
        
        float agentY = 0.2f;
        agent.ResetAgent(new Vector3(1 * cellSize, agentY, 1 * cellSize));
    }

    private void ApplyDFS(int x, int z) {
        _maze[x, z] = 0; // Mark as path

        // List of directions (jump 2 cells to leave a wall in between)
        List<Vector2Int> neighbors = new List<Vector2Int> {
            new Vector2Int(x - 2, z), new Vector2Int(x + 2, z), 
            new Vector2Int(x, z - 2), new Vector2Int(x, z + 2)
        };

        // Shuffle directions
        for (int i = 0; i < neighbors.Count; i++) {
            Vector2Int temp = neighbors[i];
            int r = Random.Range(i, neighbors.Count);
            neighbors[i] = neighbors[r];
            neighbors[r] = temp;
        }

        foreach (var next in neighbors) {
            // Check if cell is within bounds and is still a wall
            if (next.x > 0 && next.x < width - 1 && next.y > 0 && next.y < height - 1 && _maze[next.x, next.y] == 1) {
                // Remove wall between current cell and the next one
                _maze[(x + next.x) / 2, (z + next.y) / 2] = 0;
                ApplyDFS(next.x, next.y);
            }
        }
    }

    private void PlaceGoal() {
        // Find empty spot starting from the end of the maze
        for (int x = width - 2; x > 0; x--) {
            for (int z = height - 2; z > 0; z--) {
                if (_maze[x, z] == 0) {
                    Vector3 pos = new Vector3(x * cellSize, 0.4f, z * cellSize);
                    Instantiate(goalPrefab, pos, Quaternion.identity, transform);
                    return;
                }
            }
        }
    }
}