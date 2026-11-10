using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using NetworkingLab.PowerUp;

public class PacPlayerState : NetworkBehaviour
{
    public NetworkVariable<int> Score = new(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> Powered = new(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [SerializeField] private float powerSeconds = 6f;

    private float invulnerableUntil;
    private float powerEnd;

    private void Update()
    {
        if (IsServer && Powered.Value && Time.time >= powerEnd) Powered.Value = false;
    }

    public void ServerAddScore(int amount)
    {
        if (!IsServer) return;
        Score.Value = Mathf.Max(0, Score.Value + amount);
    }

    public void ServerGrantPower()
    {
        if (!IsServer) return;
        Powered.Value = true;
        powerEnd = Time.time + powerSeconds;
        AnnouncePowerRpc(GetComponent<NetworkingLab.NGO.NgoPlayerNetwork>().PlayerNumber);
    }

    [Rpc(SendTo.Everyone)]
    private void AnnouncePowerRpc(int number)
    {
        PowerUpEvents.RaiseActivated($"Player {number}");
    }

    public bool ServerTryHit()
    {
        if (!IsServer || Time.time < invulnerableUntil) return false;
        invulnerableUntil = Time.time + 2f;
        ServerAddScore(-10);
        ServerRespawn();
        return true;
    }

    public void ServerRespawn()
    {
        int number = GetComponent<NetworkingLab.NGO.NgoPlayerNetwork>().PlayerNumber;
        Vector3 p = MazeBuilder.GetSpawn(number);

        var cc = GetComponent<CharacterController>();
        cc.enabled = false;
        GetComponent<NetworkTransform>().Teleport(p, transform.rotation, transform.localScale);
        cc.enabled = true;
    }
}