using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PacGameManager : NetworkBehaviour
{
    private NetworkList<bool> eaten;
    [SerializeField] private NetworkObject ghostPrefab;
    [SerializeField] private int ghostCount = 2;
    private bool roundOver;

    private void Awake()
    {
        eaten = new NetworkList<bool>();
    }

    public override void OnNetworkSpawn()
    {
        eaten.OnListChanged += OnEatenChanged;

        if (IsServer)
        {
            for (int i = 0; i < MazeBuilder.Pellets.Count; i++) eaten.Add(false);

            for (int i = 0; i < ghostCount; i++)
            {
                var ghost = Instantiate(ghostPrefab, MazeBuilder.GetGhostSpawn(i), Quaternion.identity);
                ghost.Spawn();
            }
        }
        else
        {
            for (int i = 0; i < eaten.Count; i++) MazeBuilder.SetPelletVisible(i, !eaten[i]);
        }
    }

    public override void OnNetworkDespawn()
    {
        eaten.OnListChanged -= OnEatenChanged;
    }

    private void OnEatenChanged(NetworkListEvent<bool> e)
    {
        if (e.Type == NetworkListEvent<bool>.EventType.Value)
        {
            MazeBuilder.SetPelletVisible(e.Index, !e.Value);
            if (e.Value) AudioManager.Chomp();
        }
    }

    private int RemainingPellets()
    {
        int n = 0;
        for (int i = 0; i < eaten.Count; i++) if (!eaten[i]) n++;
        return n;
    }

    private void FixedUpdate()
    {
        if (!IsServer || !IsSpawned || roundOver) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            var player = client.PlayerObject;
            if (player == null) continue;

            Vector2Int cell = MazeBuilder.WorldToCell(player.transform.position);
            if (MazeBuilder.TryGetPellet(cell, out int index) && !eaten[index])
            {
                eaten[index] = true;
                var state = player.GetComponent<PacPlayerState>();
                if (state != null)
                {
                    if (MazeBuilder.IsPower[index])
                    {
                        state.ServerAddScore(10);
                        state.ServerGrantPower();
                    }
                    else
                    {
                        state.ServerAddScore(1);
                    }
                }
            }
        }

        if (eaten.Count > 0 && RemainingPellets() == 0) StartCoroutine(EndRound());
    }

    private IEnumerator EndRound()
    {
        roundOver = true;

        PacPlayerState best = null;
        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
        {
            var s = c.PlayerObject != null ? c.PlayerObject.GetComponent<PacPlayerState>() : null;
            if (s != null && (best == null || s.Score.Value > best.Score.Value)) best = s;
        }

        if (best != null)
        {
            int number = best.GetComponent<NetworkingLab.NGO.NgoPlayerNetwork>().PlayerNumber;
            AnnounceWinnerRpc(number, best.Score.Value);
        }

        yield return new WaitForSeconds(10f);

        for (int i = 0; i < eaten.Count; i++) eaten[i] = false;

        foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
        {
            var s = c.PlayerObject != null ? c.PlayerObject.GetComponent<PacPlayerState>() : null;
            if (s == null) continue;
            s.Score.Value = 0;
            s.ServerRespawn();
        }

        foreach (var g in FindObjectsByType<GhostEnemy>(FindObjectsSortMode.None))
            g.ServerRespawn();

        roundOver = false;
    }

    [Rpc(SendTo.Everyone)]

    private void AnnounceWinnerRpc(int number, int score)
    {
        AudioManager.Win();
        if (RoundMessageUI.Instance != null)
            RoundMessageUI.Instance.Show($"Player {number} gana con {score} puntos\nNueva ronda en 10 segundos", 9f);
    }
}