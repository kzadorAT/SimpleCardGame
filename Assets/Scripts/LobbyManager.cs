using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("Lobby List")]
    public GameObject lobbyItemPrefab;
    public GameObject lobbyPopup;
    public ScrollRect scrollView;

    [Header("Player List")]
    public GameObject playerItemPrefab;
    public GameObject playerPopup;
    public ScrollRect playerScrollView;
    public Button leaveLobbyButton;

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await SignInAnonymouslyAsync();
        }
    }

    async Task SignInAnonymouslyAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Sign in anonymously succeeded!");

            Debug.Log($"Player ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
        }
    }

    public async void CreateLobby()
    {
        var lobby = await CreateLobbyAsync();
    
        if(lobby != null)
        {
            playerPopup.SetActive(true);
            leaveLobbyButton.onClick.AddListener(async () => { await LeaveLobbyAsync(lobby.Id); }); // Agregar el listener solo una vez
            foreach (var player in lobby.Players)
            {
                var playerItem = Instantiate(playerItemPrefab, playerScrollView.content);
                playerItem.GetComponent<LobbyPlayerItem>().playerName.text = $"{player.Id}";
                playerItem.GetComponent<LobbyPlayerItem>().actualLobbyId = lobby.Id;
            }
        }
    }

    public async Task<Lobby> CreateLobbyAsync(bool isPrivate = false)
    {
        try
        {
            var lobbyName = $"Test {(isPrivate ? "Private" : "Public")} Lobby";
            var maxPlayers = 8;
            var options = new CreateLobbyOptions();
            options.IsPrivate = isPrivate;

            var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log(lobby.Name);

            StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));

            return lobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);            
            return null;
        }
    }

    public async void DeleteLobbyAsync(string lobbyId)
    {
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }

    public async void RefreshLobbiesAsync()
    {
        try
        {
            var lobbies = await QueryLobbiesAsync();

            if(lobbies != null && lobbies.Results.Count > 0)
            {
                lobbyPopup.SetActive(true);
                foreach (var lobby in lobbies.Results)
                {
                    Debug.Log(lobby.Name);
                    var lobbyItem = Instantiate(lobbyItemPrefab, scrollView.content);
                    lobbyItem.GetComponentInChildren<TMP_Text>().text = $"{lobby.Name} - {lobby.MaxPlayers} players - {lobby.AvailableSlots} available";
                    lobbyItem.GetComponent<Button>().onClick.AddListener(async () => 
                    {
                        await JoinLobbyAsync(lobby.Id);
                        lobbyPopup.SetActive(false);
                    });
                }
            }
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }

    public async Task<QueryResponse> QueryLobbiesAsync()
    {
        try
        {
            var options = new QueryLobbiesOptions
            {
                Count = 25
            };

            // Filtrar por lobby que esten disponibles
            options.Filters = new List<QueryFilter>()
            {
                new(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Ordenar por el mas reciente
            options.Order = new List<QueryOrder>()
            {
                new(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            var lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);

            return lobbies;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
            return null;
        }
    }

    public async Task<Lobby> JoinLobbyAsync(string lobbyId)
    {
        try
        {
            var lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            Debug.Log(lobby.Name);

            playerPopup.SetActive(true);
            
            leaveLobbyButton.onClick.AddListener(async () => { await LeaveLobbyAsync(lobby.Id); });
            
            foreach (var player in lobby.Players)
            {
                var playerItem = Instantiate(playerItemPrefab, playerScrollView.content);
                playerItem.GetComponent<LobbyPlayerItem>().playerName.text = $"{player.Id}";
                playerItem.GetComponent<LobbyPlayerItem>().actualLobbyId = lobbyId;
            }

            return lobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
            return null;
        }
    }

    public async Task<Lobby> JoinLobbyByCodeAsync(string lobbyCode)
    {
        try
        {
            var lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            return lobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
            return null;
        }
    }

    public async Task<Lobby> QuickJoinLobbyAsync()
    {
        try
        {
            var lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            return lobby;
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
            return null;
        }
    }
    public async Task LeaveLobbyAsync(string lobbyId)
    {
        try
        {
            var playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);

            playerPopup.SetActive(false);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogException(ex);
        }
    }

    private IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);

            yield return delay;
        }
    }

    private ConcurrentQueue<string> createdLobbyIds = new();
    void OnApplicationQuit()
    {
        while (createdLobbyIds.TryDequeue(out var lobbyId))
        {
            LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        }
    }
}
