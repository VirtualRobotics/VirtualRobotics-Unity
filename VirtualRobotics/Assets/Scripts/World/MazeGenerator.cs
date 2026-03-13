using System.Collections.Generic;
using UnityEngine;

public class MazeGenerator
{
    // Właściwość (Property) - w C# zastępuje gettery i settery z Javy. 
    // Publiczny odczyt (get), prywatny zapis (private set).
    // W C# int[,] to spójna macierz 2D w pamięci, w przeciwieństwie do int[][] z Javy (jagged array).
    public int[,] Grid { get; private set; }
    
    private readonly int _width;
    private readonly int _height;

    // Konstruktor
    public MazeGenerator(int w, int h)
    {
        // Algorytm drążenia labiryntu (DFS) wymaga nieparzystych wymiarów siatki.
        // Zapewnia to, że zawsze mamy naprzemiennie układ: ściana, ścieżka, ściana, ścieżka.
        // Jeśli podasz parzystą liczbę (np. 10), wymuszamy 11.
        _width = w % 2 == 0 ? w + 1 : w;
        _height = h % 2 == 0 ? h + 1 : h;
        
        // Inicjalizacja macierzy. Domyślnie wszystkie wartości to 0, 
        // ale zaraz to zmienimy w metodzie Generate.
        Grid = new int[_width, _height];
    }

    // Główna metoda - punkt wejścia do generacji (tzw. Orkiestrator tej klasy).
    public void Generate(bool isEmpty)
    {
        // 1. Zawsze najpierw "zalewamy" całą mapę betonem (ścianami).
        InitializeGrid();
        
        // 2. W zależności od flagi, rzeźbimy w tym betonie:
        if (isEmpty) 
            FillBorderWalls(); // Tryb pustego pokoju
        else 
            ApplyDFS(1, 1);    // Tryb labiryntu - zaczynamy rzeźbić od współrzędnych (1, 1)
    }

    // Wypełnia całą macierz jedynkami (1 = ściana). 
    // Tworzy "blok marmuru", w którym będziemy rzeźbić.
    private void InitializeGrid()
    {
        for (int x = 0; x < _width; x++)
            for (int z = 0; z < _height; z++)
                Grid[x, z] = 1;
    }

    // Główny algorytm: Randomized Depth-First Search (Rekurencyjny Backtracker).
    private void ApplyDFS(int x, int z)
    {
        // Oznaczamy obecną komórkę jako ścieżkę (0 = droga).
        Grid[x, z] = 0;
        
        // Pobieramy sąsiadów, którzy są oddaleni o DWA kroki (nie o jeden!).
        var neighbors = GetNeighbors(x, z);
        
        // Tasujemy ich. To tasowanie sprawia, że labirynt za każdym razem jest inny (losowy).
        Shuffle(neighbors);

        // Odwiedzamy każdego sąsiada po kolei...
        foreach (var next in neighbors)
        {
            // Sprawdzamy dwa warunki:
            // 1. Czy sąsiad nie wychodzi poza granice labiryntu?
            // 2. Czy sąsiad jest nadal ścianą (1)? (Jeśli jest 0, to znaczy, że już tam byliśmy).
            if (IsInBounds(next.x, next.y) && Grid[next.x, next.y] == 1)
            {
                // KLUCZOWY MOMENT ALGORYTMU:
                // Skoro idziemy do sąsiada oddalonego o 2 pola, musimy zburzyć ścianę pomiędzy nami.
                // Średnia arytmetyczna z (x) i (next.x) daje nam dokładnie współrzędną tej ściany.
                Grid[(x + next.x) / 2, (z + next.y) / 2] = 0;
                
                // Wchodzimy głębiej do tego sąsiada (rekurencja).
                ApplyDFS(next.x, next.y);
            }
        }
    }

    // Opcja alternatywna: robi pusty pokój z ramką wokół.
    private void FillBorderWalls()
    {
        for (int x = 0; x < _width; x++)
            for (int z = 0; z < _height; z++)
                // Jeśli komórka jest na samym skraju (x=0, z=0, lub maks), dajemy 1 (ściana).
                // Reszta w środku dostaje 0 (podłoga).
                Grid[x, z] = (x == 0 || z == 0 || x == _width - 1 || z == _height - 1) ? 1 : 0;
    }

    // Zwraca listę 4 sąsiadów. Zauważ, że przesuwamy się o 2 (-2, +2), 
    // aby zawsze przeskakiwać przez potencjalną ścianę działową.
    private List<Vector2Int> GetNeighbors(int x, int z) => new List<Vector2Int>
    {
        new Vector2Int(x - 2, z), new Vector2Int(x + 2, z),
        new Vector2Int(x, z - 2), new Vector2Int(x, z + 2)
    };

    // Klasyczny algorytm tasowania (Fisher-Yates) z wykorzystaniem Unity Random.
    private void Shuffle(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            // Elegancki "Tuple swap" (nowość w C# 7.0). 
            // Zamienia miejscami list[i] z list[r] bez pisania zmiennej tymczasowej (temp).
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    // Sprawdza, czy koordynaty są wewnątrz dozwolonego obszaru.
    // Zauważ, że nie sprawdzamy "x >= 0", tylko "x > 0". 
    // Robimy to celowo, aby zostawić nieruszalną ramkę ścian zewnętrznych na obrzeżach.
    private bool IsInBounds(int x, int z) => x > 0 && x < _width - 1 && z > 0 && z < _height - 1;
}