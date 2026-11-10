using System.Text;
using UnityEngine;

public class ScoreboardUI : MonoBehaviour
{
    private float nextRefresh;
    private string text = "";

    private void Update()
    {
        if (Time.time < nextRefresh) return;
        nextRefresh = Time.time + 0.25f;

        var players = FindObjectsByType<PacPlayerState>(FindObjectsSortMode.None);
        System.Array.Sort(players, (a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));

        var sb = new StringBuilder();
        for (int i = 0; i < players.Length; i++)
            sb.AppendLine($"P{i + 1}: {players[i].Score.Value}");
        text = sb.ToString();
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width - 320, 20, 300, 300), text, style);
    }
}