using UnityEngine;
using UnityEngine.UI;

public class UiTheme : MonoBehaviour
{
    private void Start()
    {
        Color cyan = new Color32(0, 240, 255, 255);
        Color magenta = new Color32(255, 43, 214, 255);

        foreach (var b in GetComponentsInChildren<Button>(true))
        {
            var cb = b.colors;
            cb.normalColor = new Color(0.05f, 0.12f, 0.45f, 1f);
            cb.highlightedColor = new Color(0.0f, 0.55f, 0.8f, 1f);
            cb.pressedColor = new Color(0.8f, 0.1f, 0.65f, 1f);
            cb.selectedColor = cb.normalColor;
            b.colors = cb;

            var ol = b.gameObject.AddComponent<Outline>();
            ol.effectColor = cyan;
            ol.effectDistance = new Vector2(2, -2);

            var t = b.GetComponentInChildren<Text>();
            if (t != null) { t.color = Color.white; t.fontStyle = FontStyle.Bold; }
        }

        foreach (var f in GetComponentsInChildren<InputField>(true))
        {
            if (f.image != null) f.image.color = new Color(0.04f, 0.06f, 0.2f, 0.95f);
            if (f.textComponent != null) f.textComponent.color = cyan;

            var ol = f.gameObject.AddComponent<Outline>();
            ol.effectColor = magenta;
            ol.effectDistance = new Vector2(2, -2);
        }
    }
}