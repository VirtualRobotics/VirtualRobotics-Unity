using UnityEngine;
using System.Collections.Generic;

public class MazeManager : MonoBehaviour
{
    public static MazeManager Instance;

    [Header("Ustawienia Labiryntu")]
    public int width = 11;
    public int height = 11;
    public float cellSize = 1f;

    [Header("Prefaby i Referencje")]
    public HeuristicMovement agent;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject goalPrefab;

    private int[,] _maze;

    void Awake() => Instance = this;

    void Start() => GenerateNewLevel();

    public void GenerateNewLevel()
    {
        // 1. Czyszczenie starych obiektów
        foreach (Transform child in transform) {
            if (child.gameObject.CompareTag("Agent")) continue;
            Destroy(child.gameObject);
        }

        // 2. Inicjalizacja tablicy
        _maze = new int[width, height];
        for (int x = 0; x < width; x++) {
            for (int z = 0; z < height; z++) {
                _maze[x, z] = 1;
            }
        }

        // 3. Algorytm DFS - drąży tunele (0) w ścianach (1)
        ApplyDFS(1, 1);

        // 4. Budowanie fizyczne
        for (int x = 0; x < width; x++) {
            for (int z = 0; z < height; z++) {
                Vector3 pos = new Vector3(x * cellSize, 0, z * cellSize);
                
                GameObject floor = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                floor.transform.localScale = new Vector3(cellSize * 0.1f, 1f, cellSize * 0.1f);

                if (_maze[x, z] == 1) {
                    // ŚCIANA (Prefab 1x1x1)
                    Vector3 wallPos = pos + Vector3.up * (cellSize / 2f);
                    GameObject wall = Instantiate(wallPrefab, wallPos, Quaternion.identity, transform);
                    wall.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
                }
            }
        }

        // 5. Umieszczenie celu i reset agenta
        PlaceGoal();
        
        float agentY = 0.2f;
        agent.ResetAgent(new Vector3(1 * cellSize, agentY, 1 * cellSize));
    }

    private void ApplyDFS(int x, int z) {
        _maze[x, z] = 0; // Oznacz jako ścieżkę

        // Lista kierunków (skok o 2 pola, żeby zostawić ścianę pomiędzy)
        List<Vector2Int> neighbors = new List<Vector2Int> {
            new Vector2Int(x - 2, z), new Vector2Int(x + 2, z), 
            new Vector2Int(x, z - 2), new Vector2Int(x, z + 2)
        };

        // Tasowanie kierunków
        for (int i = 0; i < neighbors.Count; i++) {
            Vector2Int temp = neighbors[i];
            int r = Random.Range(i, neighbors.Count);
            neighbors[i] = neighbors[r];
            neighbors[r] = temp;
        }

        foreach (var next in neighbors) {
            // Sprawdź czy pole jest wewnątrz granic i czy jest jeszcze ścianą
            if (next.x > 0 && next.x < width - 1 && next.y > 0 && next.y < height - 1 && _maze[next.x, next.y] == 1) {
                // Usuń ścianę pomiędzy aktualnym polem a następnym
                _maze[(x + next.x) / 2, (z + next.y) / 2] = 0;
                ApplyDFS(next.x, next.y);
            }
        }
    }

    private void PlaceGoal() {
        // Szukamy wolnego miejsca od końca labiryntu
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