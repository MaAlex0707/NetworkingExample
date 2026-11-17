using UnityEngine;

using System.Collections.Generic;
using UnityEngine;

public class MazeBuilder : MonoBehaviour
{
    public const float Cell = 1.2f;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material pelletMaterial;
    [SerializeField] private Material powerMaterial;
    [SerializeField] private Sprite pelletSprite;
    [SerializeField] private Sprite powerSprite;
    [SerializeField] private Sprite wallSprite;

    // # pared   . pellet   o power pellet   (espacio) camino vacio
    static readonly string[] Map =
    {
        "#####################",
        "#o.................o#",
        "#.###.##.#.#.##.###.#",
        "#...................#",
        "#.###.#.#####.#.###.#",
        "#.....#...#...#.....#",
        "#####.###.#.###.#####",
        "#.......#...#.......#",
        "#####.###.#.###.#####",
        "#.....#...#...#.....#",
        "#.###.#.#####.#.###.#",
        "#...................#",
        "#.###.##.#.#.##.###.#",
        "#o.................o#",
        "#####################",
    };

    public static int Width => Map[0].Length;
    public static int Height => Map.Length;

    public static readonly List<GameObject> Pellets = new();
    public static readonly List<bool> IsPower = new();
    static readonly Dictionary<Vector2Int, int> pelletAt = new();

    public static Vector3 CellToWorld(Vector2Int c) =>
        new((c.x - (Width - 1) / 2f) * Cell, 0f, ((Height - 1) / 2f - c.y) * Cell);

    public static Vector2Int WorldToCell(Vector3 p) => new(
        Mathf.RoundToInt(p.x / Cell + (Width - 1) / 2f),
        Mathf.RoundToInt((Height - 1) / 2f - p.z / Cell));

    public static bool TryGetPellet(Vector2Int c, out int index) => pelletAt.TryGetValue(c, out index);

    public static bool IsWall(Vector2Int c) =>
    c.x < 0 || c.y < 0 || c.y >= Height || c.x >= Map[c.y].Length || Map[c.y][c.x] == '#';

    public static Vector3 GhostSpawn => CellToWorld(new Vector2Int(10, 7)) + Vector3.up * 0.75f;

    public static Vector3 GetGhostSpawn(int index)
    {
        Vector2Int[] cells = { new(9, 7), new(11, 7) };
        return CellToWorld(cells[index % cells.Length]) + Vector3.up * 0.75f;
    }

    public static void SetPelletVisible(int i, bool visible)
    {
        if (i >= 0 && i < Pellets.Count) Pellets[i].SetActive(visible);
    }

    public static Vector3 GetSpawn(int playerNumber)
    {
        Vector2Int[] corners =
        {
            new(5, 3),
            new(15, 3),
            new(5, 11),
            new(15, 11),
        };
        int i = (Mathf.Max(1, playerNumber) - 1) % 4;
        return CellToWorld(corners[i]) + Vector3.up * 0.75f;
    }

    void Awake()
    {
        Pellets.Clear();
        IsPower.Clear();
        pelletAt.Clear();

        for (int r = 0; r < Height; r++)
        {
            for (int c = 0; c < Map[r].Length; c++)
            {
                char ch = Map[r][c];
                var cell = new Vector2Int(c, r);
                Vector3 pos = CellToWorld(cell);

                if (ch == '#')
                {
                    GameObject wall = new GameObject("Wall");
                    wall.transform.SetParent(transform);
                    wall.transform.position = pos + Vector3.up * 0.75f;

                    var box = wall.AddComponent<BoxCollider>();
                    box.size = new Vector3(Cell, 1f, Cell);

                    GameObject visual = new GameObject("Visual");
                    visual.transform.SetParent(wall.transform, false);
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                    var sr = visual.AddComponent<SpriteRenderer>();
                    sr.sprite = wallSprite;
                    sr.sortingOrder = 0;
                }
                else if (ch == '.' || ch == 'o')
                {
                    bool power = ch == 'o';
                    GameObject p = new GameObject(power ? "PowerPellet" : "Pellet");
                    p.transform.SetParent(transform);
                    p.transform.position = pos + Vector3.up * 0.75f;
                    p.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // acostado, mirando a la cámara
                    p.transform.localScale = Vector3.one * 1.5f;

                    var sr = p.AddComponent<SpriteRenderer>();
                    sr.sprite = power ? powerSprite : pelletSprite;
                    sr.sortingOrder = 1;

                    pelletAt[cell] = Pellets.Count;
                    Pellets.Add(p);
                    IsPower.Add(power);
                }
            }
        }
    }
}