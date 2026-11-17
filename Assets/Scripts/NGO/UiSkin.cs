using UnityEngine;
using UnityEngine.UI;

public class UiSkin : MonoBehaviour
{
    [SerializeField] private Image panelImage;
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite inputSprite;

    [SerializeField] private Sprite connectNormal, connectHover;
    [SerializeField] private Sprite shutdownNormal, shutdownHover;
    [SerializeField] private Sprite serverNormal, serverHover;
    [SerializeField] private Sprite hostNormal, hostHover;

    private void Start()
    {
        if (panelImage != null && panelSprite != null)
        {
            panelImage.sprite = panelSprite;
            panelImage.type = Image.Type.Simple;
            panelImage.color = Color.white;
        }

        foreach (var b in GetComponentsInChildren<Button>(true))
        {
            var label = b.GetComponentInChildren<Text>();
            string t = label != null ? label.text.ToLower() : "";

            Sprite normal = null, hover = null;
            if (t.Contains("connect")) { normal = connectNormal; hover = connectHover; }
            else if (t.Contains("shutdown")) { normal = shutdownNormal; hover = shutdownHover; }
            else if (t.Contains("server")) { normal = serverNormal; hover = serverHover; }
            else if (t.Contains("host")) { normal = hostNormal; hover = hostHover; }
            if (normal == null) continue;

            var img = b.GetComponent<Image>();
            img.sprite = normal;
            img.type = Image.Type.Simple;
            img.color = Color.white;

            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState
            {
                highlightedSprite = hover,
                pressedSprite = hover,
                selectedSprite = normal,
                disabledSprite = normal
            };

            if (label != null)
            {
                label.color = Color.white;
                label.fontStyle = FontStyle.Bold;
            }
        }

        foreach (var f in GetComponentsInChildren<InputField>(true))
        {
            if (f.image != null && inputSprite != null)
            {
                f.image.sprite = inputSprite;
                f.image.type = Image.Type.Simple;
                f.image.color = Color.white;
            }
            if (f.textComponent != null)
            {
                f.textComponent.color = Color.white;
                f.textComponent.fontStyle = FontStyle.Bold;
            }
        }
    }
}