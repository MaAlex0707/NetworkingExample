using UnityEngine;

public class PlayerSpriteView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] cyanFrames;
    [SerializeField] private Sprite[] magentaFrames;
    [SerializeField] private Sprite[] limeFrames;
    [SerializeField] private Sprite[] orangeFrames;
    [SerializeField] private float spriteScale = 1.2f;

    private NetworkingLab.NGO.NgoPlayerNetwork identity;
    private PacPlayerState state;
    private Vector3 lastPos;
    private Vector3 facing = Vector3.right;
    private float lastMoveTime = -10f;

    private void Awake()
    {
        identity = GetComponent<NetworkingLab.NGO.NgoPlayerNetwork>();
        state = GetComponent<PacPlayerState>();
        lastPos = transform.position;
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        Vector3 delta = transform.position - lastPos;
        delta.y = 0f;
        if (delta.sqrMagnitude > 0.00001f)
        {
            lastMoveTime = Time.time;
            facing = delta.normalized;
        }
        lastPos = transform.position;
        bool moving = Time.time - lastMoveTime < 0.15f;

        Sprite[] frames = identity.PlayerNumber switch
        {
            2 => magentaFrames,
            3 => limeFrames,
            4 => orangeFrames,
            _ => cyanFrames,
        };
        if (frames == null || frames.Length < 3) return;

        int[] cycle = { 0, 1, 2, 1 };
        int i = moving ? cycle[(int)(Time.time * 10f) % cycle.Length] : 1;
        spriteRenderer.sprite = frames[i];

        // Izquierda/derecha: espejo. Arriba/abajo: giro de 90. Nunca de cabeza.
        float angle = 0f;
        bool flip = false;
        if (Mathf.Abs(facing.x) >= Mathf.Abs(facing.z)) flip = facing.x < 0f;
        else angle = facing.z > 0f ? -90f : 90f;

        spriteRenderer.flipX = flip;
        spriteRenderer.transform.rotation =
            Quaternion.Euler(0f, angle, 0f) * Quaternion.Euler(90f, 0f, 0f);

        float pulse = (state != null && state.Powered.Value)
            ? 1f + 0.15f * Mathf.Sin(Time.time * 12f) : 1f;
        spriteRenderer.transform.localScale = Vector3.one * spriteScale * pulse;
    }
}