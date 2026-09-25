// LevelGenerator.cs - Unity 6 (C#)
// Adjuntar a un GameObject vacío llamado "Generator" en una escena vacía
using UnityEngine;
using System.Collections.Generic;
using System.Text;

public class LevelGenerator : MonoBehaviour
{
    public int width = 30, height = 20;
    [Range(0.1f,0.9f)] public float fillPercent = 0.45f;
    public int minRooms = 3;
    public int minSpawnExitDist = 10;
    public float rewardDensity = 0.05f;
    public float minRewardDist = 3f;
    char[,] grid;
    Vector2Int spawn, exit;
    GameObject tileParent;

    void Start() { Regenerate(); }
    void Update() { if (Input.GetKeyDown(KeyCode.R)) Regenerate(); }

    public void Regenerate()
    {
        bool valid = false; int attempts = 0;
        while (!valid && attempts < 50)
        {
            GenerateGrid();          // PARTE 1
            valid = ApplyConstraints(); // PARTE 2
            attempts++;
        }
        Debug.Log($"Mapa generado en {attempts} intentos. Valido: {valid}");
        PrintGrid();
        DrawTiles();
    }

    // PARTE 1: Drunkard's Walk
    void GenerateGrid()
    {
        grid = new char[width, height];
        for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) grid[x, y] = '#';
        int total = width * height;
        int targetFloor = Mathf.RoundToInt(total * fillPercent);
        int floorCount = 0;
        Vector2Int pos = new Vector2Int(width / 2, height / 2);
        grid[pos.x, pos.y] = '.'; floorCount++;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int safety = 0;
        while (floorCount < targetFloor && safety < total * 20)
        {
            safety++;
            Vector2Int next = pos + dirs[Random.Range(0, 4)];
            if (next.x > 0 && next.x < width - 1 && next.y > 0 && next.y < height - 1)
            {
                pos = next;
                if (grid[pos.x, pos.y] == '#') { grid[pos.x, pos.y] = '.'; floorCount++; }
            }
        }
        spawn = pos;
        grid[spawn.x, spawn.y] = 'S';
    }

    // PARTE 2: validacion de restricciones de jugabilidad
    bool ApplyConstraints()
    {
        // Flood fill CORREGIDO: usa matriz de visitados (evita falso positivo de conectividad)
        bool[,] visited = new bool[width, height];
        FloodFill(spawn, visited);
        if (CountRooms(visited) < minRooms) return false;

        Dictionary<Vector2Int, int> dist = BFSDistances(spawn);
        Vector2Int bestExit = spawn; int bestDist = -1;
        foreach (var kv in dist)
            if (grid[kv.Key.x, kv.Key.y] == '.' && kv.Value > bestDist) { bestDist = kv.Value; bestExit = kv.Key; }
        if (bestDist < minSpawnExitDist) return false;
        exit = bestExit; grid[exit.x, exit.y] = 'E';

        PlaceRewards(dist); // recompensas solo en zonas alcanzables
        return true;
    }

    List<Vector2Int> FloodFill(Vector2Int start, bool[,] visited)
    {
        List<Vector2Int> region = new List<Vector2Int>();
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(start);
        visited[start.x, start.y] = true; // marcar visitado al encolar, corrige el bug clasico
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (q.Count > 0)
        {
            Vector2Int c = q.Dequeue(); region.Add(c);
            foreach (var d in dirs)
            {
                Vector2Int n = c + d;
                if (n.x >= 0 && n.x < width && n.y >= 0 && n.y < height && !visited[n.x, n.y] && grid[n.x, n.y] != '#')
                { visited[n.x, n.y] = true; q.Enqueue(n); }
            }
        }
        return region;
    }

    int CountRooms(bool[,] reachable)
    {
        bool[,] seen = new bool[width, height]; int rooms = 0;
        for (int x = 0; x < width; x++) for (int y = 0; y < height; y++)
            if (reachable[x, y] && !seen[x, y] && grid[x, y] != '#')
            { if (FloodFill(new Vector2Int(x, y), seen).Count >= 2) rooms++; }
        return rooms;
    }

    Dictionary<Vector2Int, int> BFSDistances(Vector2Int start)
    {
        var dist = new Dictionary<Vector2Int, int>(); var q = new Queue<Vector2Int>();
        dist[start] = 0; q.Enqueue(start);
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            foreach (var d in dirs)
            {
                var n = c + d;
                if (n.x >= 0 && n.x < width && n.y >= 0 && n.y < height && grid[n.x, n.y] != '#' && !dist.ContainsKey(n))
                { dist[n] = dist[c] + 1; q.Enqueue(n); }
            }
        }
        return dist;
    }

    // MEJORA: Colocar las reconpenzas lo mas lejos posible para umnentar la dificultad
    void PlaceRewards(Dictionary<Vector2Int, int> dist)
    {
        var placed = new List<Vector2Int>();
        int maxRewards = Mathf.RoundToInt(dist.Count * rewardDensity);
        var candidates = new List<Vector2Int>(dist.Keys);
        candidates.Sort((a, b) => dist[b].CompareTo(dist[a])); // comenzar desde el punto mas lejano
        foreach (var c in candidates)
        {
            if (placed.Count >= maxRewards) break;
            if (grid[c.x, c.y] != '.') continue;
            bool tooClose = false;
            foreach (var p in placed) if (Vector2Int.Distance(p, c) < minRewardDist) { tooClose = true; break; }
            if (tooClose) continue;
            grid[c.x, c.y] = '$'; placed.Add(c); // valor implicito = dist[c]
        }
    }

    void PrintGrid()
    {
        StringBuilder sb = new StringBuilder();
        for (int y = 0; y < height; y++) { for (int x = 0; x < width; x++) sb.Append(grid[x, y]); sb.Append('\n'); }
        Debug.Log(sb.ToString());
    }

    void DrawTiles()
    {
        if (tileParent != null) Destroy(tileParent);
        tileParent = new GameObject("Tiles");
        for (int x = 0; x < width; x++) for (int y = 0; y < height; y++)
        {
            char c = grid[x, y]; if (c == '#') continue;
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(x, 0, y);
            cube.transform.parent = tileParent.transform;
            var r = cube.GetComponent<Renderer>();
            if (c == 'S') r.material.color = Color.blue;
            else if (c == 'E') r.material.color = Color.red;
            else if (c == '$') r.material.color = Color.yellow;
            else r.material.color = Color.gray;
        }
    }

    void OnGUI() { if (GUI.Button(new Rect(10, 10, 150, 30), "Regenerar")) Regenerate(); }
}
