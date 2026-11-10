using UnityEngine;

public class RoundMessageUI : MonoBehaviour
{
    public static RoundMessageUI Instance;

    private string message = "";
    private float hideAt;

    private void Awake()
    {
        Instance = this;
    }

    public void Show(string text, float seconds)
    {
        message = text;
        hideAt = Time.time + seconds;
    }

    private void OnGUI()
    {
        if (Time.time > hideAt || string.IsNullOrEmpty(message)) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.yellow;

        var rect = new Rect(0, Screen.height * 0.4f, Screen.width, 120);
        GUI.Label(rect, message, style);
    }
}