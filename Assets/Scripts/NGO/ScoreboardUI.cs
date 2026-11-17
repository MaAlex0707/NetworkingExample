using System.Collections.Generic;
using UnityEngine;

public class ScoreboardUI : MonoBehaviour
{
    private static readonly Color[] PlayerColors =
    {
        new Color32(0, 240, 255, 255),
        new Color32(255, 43, 214, 255),
        new Color32(140, 255, 26, 255),
        new Color32(255, 154, 31, 255),
    };

    private struct Row { public int number; public int score; }

    private readonly List<Row> rows = new();
    private float nextRefresh;
    private Texture2D white;

    private void Awake()
    {
        white = new Texture2D(1, 1);
        white.SetPixel(0, 0, Color.white);
        white.Apply();
    }

    private void Update()
    {
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + 0.2f;

        rows.Clear();
        var players = FindObjectsByType<PacPlayerState>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            var id = p.GetComponent<NetworkingLab.NGO.NgoPlayerNetwork>();
            if (id == null || id.PlayerNumber <= 0) continue;
            rows.Add(new Row { number = id.PlayerNumber, score = p.Score.Value });
        }
        rows.Sort((a, b) => a.number.CompareTo(b.number));
    }

    private void Box(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, white);
        GUI.color = Color.white;
    }

    private void OnGUI()
    {
        if (rows.Count == 0) return;

        float s = Screen.height / 1080f;
        float w = 340f * s, rowH = 60f * s, head = 64f * s, pad = 14f * s;
        float h = head + rows.Count * rowH + pad;
        float x = Screen.width - w - 24f * s, y = 24f * s;

        // Panel con borde neón
        Box(new Rect(x - 3 * s, y - 3 * s, w + 6 * s, h + 6 * s), new Color(0.1f, 0.5f, 1f, 0.9f));
        Box(new Rect(x, y, w, h), new Color(0.03f, 0.04f, 0.14f, 0.92f));

        var title = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(30 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        title.normal.textColor = new Color(0.7f, 0.9f, 1f);
        GUI.Label(new Rect(x, y, w, head), "PUNTAJE", title);

        int best = 0;
        foreach (var r in rows) best = Mathf.Max(best, r.score);

        var name = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(32 * s),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        var num = new GUIStyle(name) { alignment = TextAnchor.MiddleRight };

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            Color c = PlayerColors[(r.number - 1) % 4];
            float ry = y + head + i * rowH;
            bool leader = r.score == best && best > 0;

            Box(new Rect(x + pad, ry + 4 * s, w - 2 * pad, rowH - 8 * s),
                leader ? new Color(c.r, c.g, c.b, 0.28f) : new Color(1, 1, 1, 0.06f));
            Box(new Rect(x + pad, ry + 4 * s, 8 * s, rowH - 8 * s), c);

            name.normal.textColor = c;
            num.normal.textColor = Color.white;
            GUI.Label(new Rect(x + pad + 24 * s, ry, w, rowH), $"P{r.number}", name);
            GUI.Label(new Rect(x, ry, w - pad - 16 * s, rowH), r.score.ToString(), num);
        }
    }
}