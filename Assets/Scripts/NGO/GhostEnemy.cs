using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class GhostEnemy : NetworkBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private Renderer body;
    [SerializeField] private Color normalColor = Color.red;
    [SerializeField] private Color scaredColor = Color.blue;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] redFrames;
    [SerializeField] private Sprite[] violetFrames;
    [SerializeField] private Sprite[] scaredFrames;

    public NetworkVariable<bool> Scared = new(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> Variant = new(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private static int nextVariant;

    private static readonly Vector2Int[] Dirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1)
    };

    private Vector2Int cell;
    private Vector2Int dir;
    private Vector3 target;
    private float y;

    public override void OnNetworkSpawn()
    {
        Scared.OnValueChanged += (_, v) => Paint(v);
        Paint(Scared.Value);

        if (!IsServer) return;

        Variant.Value = nextVariant++ % 2;

        y = transform.position.y;
        cell = MazeBuilder.WorldToCell(transform.position);
        target = MazeBuilder.CellToWorld(cell) + Vector3.up * y;
    }

    private void Paint(bool scared)
    {
        if (body != null) body.material.color = scared ? scaredColor : normalColor;
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        Sprite[] frames = Scared.Value ? scaredFrames : (Variant.Value == 0 ? redFrames : violetFrames);
        if (frames == null || frames.Length == 0) return;

        int i = (int)(Time.time * 4f) % frames.Length;
        spriteRenderer.sprite = frames[i];
    }

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned) return;

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.fixedDeltaTime);

        if ((transform.position - target).sqrMagnitude < 0.0004f)
        {
            ChooseNext();
        }

        CheckHits();
    }

    private void CheckHits()
    {
        bool anyPowered = false;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var po = client.PlayerObject;
            if (po == null) continue;

            var st = po.GetComponent<PacPlayerState>();
            if (st == null) continue;
            anyPowered |= st.Powered.Value;

            if ((po.transform.position - transform.position).sqrMagnitude < 0.7f * 0.7f)
            {
                if (st.Powered.Value)
                {
                    st.ServerAddScore(50);
                    st.ServerGhostEaten();
                    ServerRespawn();
                }
                else
                {
                    st.ServerTryHit();
                }
            }
        }

        if (Scared.Value != anyPowered) Scared.Value = anyPowered;
    }

    public void ServerRespawn()
    {
        Vector3 sp = MazeBuilder.GetGhostSpawn(Variant.Value);
        var nt = GetComponent<NetworkTransform>();
        nt.Teleport(sp, Quaternion.identity, transform.localScale);
        cell = MazeBuilder.WorldToCell(sp);
        target = MazeBuilder.CellToWorld(cell) + Vector3.up * y;
        dir = Vector2Int.zero;
    }

    private void ChooseNext()
    {
        var options = new List<Vector2Int>();
        foreach (var d in Dirs)
        {
            if (d != -dir && !MazeBuilder.IsWall(cell + d)) options.Add(d);
        }
        if (options.Count == 0) options.Add(-dir);

        float wander = Variant.Value == 0 ? 0.2f : 0.45f;

        if (Random.value < wander)
        {
            dir = options[Random.Range(0, options.Count)];
        }
        else
        {
            Vector3 p = NearestPlayer();
            float best = float.MaxValue;
            foreach (var d in options)
            {
                float dist = (MazeBuilder.CellToWorld(cell + d) - p).sqrMagnitude;
                if (Scared.Value) dist = -dist;
                if (dist < best) { best = dist; dir = d; }
            }
        }

        cell += dir;
        target = MazeBuilder.CellToWorld(cell) + Vector3.up * y;
    }

    private Vector3 NearestPlayer()
    {
        Vector3 best = transform.position;
        float bd = float.MaxValue;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var po = client.PlayerObject;
            if (po == null) continue;
            float d = (po.transform.position - transform.position).sqrMagnitude;
            if (d < bd) { bd = d; best = po.transform.position; }
        }
        return best;
    }
}