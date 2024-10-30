using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class TurnManager : NetworkBehaviour
{
    [Header("Visuals")]
    public TMP_Text turnIndicator;

    private ulong currentTurnPlayerId;
    private float turnTimer;
    private float turnDuration = 30f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentTurnPlayerId = NetworkManager.Singleton.ConnectedClientsIds.First();
            StartTurn();
        }
    }

    private void Update()
    {
        if(IsServer)
        {
            if(turnTimer > 0)
            {
                turnTimer -= Time.deltaTime;
                if(turnTimer <= 0)
                {
                    EndTurn();
                }
            }
        }
    }

    [ServerRpc]
    public void EndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        if(!IsServer) return;

        var clientId = rpcParams.Receive.SenderClientId;
        if(clientId == currentTurnPlayerId)
        {
            EndTurn();
        }
    }

    private void StartTurn()
    {
        turnTimer = turnDuration;
        UpdateTurnIndicatorClientRpc(currentTurnPlayerId);
    }

    private void EndTurn()
    {
        var connectedClients = NetworkManager.ConnectedClientsIds;
        var currentIndex = connectedClients.FirstOrDefault(clientId => clientId == currentTurnPlayerId);
        currentTurnPlayerId = connectedClients[(int)(currentIndex + 1) % connectedClients.Count];
        StartTurn();
    }

    [ClientRpc]
    private void UpdateTurnIndicatorClientRpc(ulong clientId)
    {
        turnIndicator.text = $"Turn: {clientId}";
    }
}
