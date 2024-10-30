using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class CardGameManager : NetworkBehaviour
{
    private NetworkList<CardData> deck;
    private NetworkList<CardData> discardPile;
    private Dictionary<ulong, List<CardData>> playerHands;
    private Dictionary<ulong, NetworkList<CardData>> playerDiscardPiles;

    [SerializeField]
    private GameObject cardPrefab;

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            deck = new ();
            discardPile = new ();
            playerHands = new ();
            playerDiscardPiles = new ();
        }
    }

    [ServerRpc]
    public void InitializeDeckServerRpc(int numberOfDecks)
    {
        if(!IsServer) return;

        deck.Clear();
        for(var d = 0; d < numberOfDecks; d++)
        {
            for(var suit = 0; suit < 4; suit++)
            {
                for(var value = 1; value <= 13; value++)
                {
                    deck.Add(new CardData
                    {
                        Value = value,
                        SuitIndex = suit,
                        IsFaceUp = false,
                        OwnerId = 0,
                    });
                }
            }
        }

        ShuffleDeckServerRpc();
    }

    [ServerRpc]
    public void ShuffleDeckServerRpc()
    {
        if(!IsServer) return;

        List<CardData> tempDeck = new ();

        foreach(var card in deck)
        {
            tempDeck.Add(card);
        }

        var n = tempDeck.Count;

        while(n > 1)
        {
            n--;
            var k = Random.Range(0, n + 1);
            var temp = tempDeck[k];
            tempDeck[k] = tempDeck[n];
            tempDeck[n] = temp;
        }
        deck.Clear();
        foreach(var card in tempDeck)
        {
            deck.Add(card);
        }
    }

    [ServerRpc]
    public void DealCardsServerRpc(int cardsPerPlayer)
    {
        if(!IsServer) return;

        foreach(var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if(!playerHands.ContainsKey(clientId))
            {
                playerHands[clientId] = new ();
            }

            for(var i = 0; i < cardsPerPlayer && deck.Count > 0; i++)
            {
                var cardData = deck[deck.Count - 1];
                cardData.OwnerId = clientId;

                // Spawn el visual de la carta para todos los clientes
                GameObject cardObj = SpawnCardVisual(cardData);
                cardData.NetworkObjectId = cardObj.GetComponent<NetworkObject>().NetworkObjectId;

                deck.RemoveAt(deck.Count - 1);
                playerHands[clientId].Add(cardData);
                UpdatePlayerHandClientRpc(clientId, cardData);
            }
        }
    }

    private GameObject SpawnCardVisual(CardData cardData)
    {
        GameObject cardObj = Instantiate(cardPrefab);
        var networkObj = cardObj.GetComponent<NetworkObject>();
        networkObj.Spawn();

        var cardVisual = cardObj.GetComponent<CardVisual>();
        cardVisual.Initialize(cardData);

        return cardObj;
    }

    [ServerRpc]
    public void DrawCardServerRpc(ServerRpcParams rpcParams = default)
    {
        if(!IsServer) return;

        var clientId = rpcParams.Receive.SenderClientId;
        if(deck.Count > 0)
        {
            var cardData = deck[deck.Count - 1];
            cardData.OwnerId = clientId;

            GameObject cardObj = SpawnCardVisual(cardData);
            cardData.NetworkObjectId = cardObj.GetComponent<NetworkObject>().NetworkObjectId;

            deck.RemoveAt(deck.Count - 1);

            if(!playerHands.ContainsKey(clientId))
            {
                playerHands[clientId] = new ();
            }

            playerHands[clientId].Add(cardData);
            UpdatePlayerHandClientRpc(clientId, cardData);
        }
    }

    [ServerRpc]
    public void DiscardCardServerRpc(int cardIndex, bool toUniversalPile, ServerRpcParams rpcParams = default)
    {
        if(!IsServer) return;

        var clientId = rpcParams.Receive.SenderClientId;
        if(playerHands.ContainsKey(clientId) && cardIndex < playerHands[clientId].Count)
        {
            var cardData = playerHands[clientId][cardIndex];
            playerHands[clientId].RemoveAt(cardIndex);

            if(toUniversalPile)
            {
                discardPile.Add(cardData);
            }
            else
            {
                if(!playerDiscardPiles.ContainsKey(clientId))
                {
                    playerDiscardPiles[clientId] = new ();
                }
                playerDiscardPiles[clientId].Add(cardData);
            }

            UpdateDiscardPileClientRpc(clientId, cardData, toUniversalPile);
            UpdateCardPositionClientRpc(cardData.NetworkObjectId, toUniversalPile);
        }
    }

    [ServerRpc]
    public void FlipCardServerRpc(int cardIndex, ServerRpcParams rpcParams = default)
    {
        if(!IsServer) return;

        var clientId = rpcParams.Receive.SenderClientId;
        if(playerHands.ContainsKey(clientId) && cardIndex < playerHands[clientId].Count)
        {
            var cardData = playerHands[clientId][cardIndex];
            cardData.IsFaceUp = !cardData.IsFaceUp;
            UpdateCardStateClientRpc(cardData);
        }
    }

    [ClientRpc]
    private void UpdatePlayerHandClientRpc(ulong clientId, CardData cardData)
    {
        // Implementar actualizacion de la UI para la mano del jugador
    }

    [ClientRpc]
    private void UpdateDiscardPileClientRpc(ulong clientId, CardData cardData, bool toUniversalPile)
    {
        // Implementar actualizacion de la UI para la pila de descarte
    }

    [ClientRpc]
    private void UpdateCardStateClientRpc(CardData cardData)
    {
        // Encontrar el objeto de la carta por NetworkObjectId y actualizar su estado
        var cardObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[cardData.NetworkObjectId];
        if(cardObj != null)
        {
            var cardVisual = cardObj.GetComponent<CardVisual>();
            cardVisual.Initialize(cardData);
        }
    }

    [ClientRpc]
    private void UpdateCardPositionClientRpc(ulong networkObjectId, bool toUniversalPile)
    {
        // Mover el visual de la carta a la posicion correcta
        var cardObj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
        if(cardObj != null)
        {
            Vector3 targetPosition = toUniversalPile ? GetUniversalDiscardPilePosition() : GetPlayerDiscardPilePosition();

            // Implementar la logica de movimiento (puede ser inmediata o animada)
            cardObj.transform.position = targetPosition;            
        }
    }

    private Vector3 GetPlayerDiscardPilePosition()
    {
        // Implementar la logica para obtener la posicion de la pila universal
        return Vector3.zero;
    }

    private Vector3 GetUniversalDiscardPilePosition()
    {
        // Implementar la logica para obtener la posicion de la pila universal
        return Vector3.zero;
    }
}
