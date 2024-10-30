using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CardVisual : NetworkBehaviour
{
    [Header("Visuals")]
    public TMP_Text topValueText;
    public TMP_Text bottomValueText;
    public TMP_Text suitText;
    public GameObject foregroundPanel;

    private NetworkVariable<CardData> cardData = new();

    public void Initialize(CardData data)
    {
        cardData.Value = data;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Implementar la actualización de la visual de la carta
        var suit = GetSuitFromIndex(cardData.Value.SuitIndex);

        topValueText.text = cardData.Value.Value.ToString();
        bottomValueText.text = cardData.Value.Value.ToString();
        suitText.text = suit;
        foregroundPanel.SetActive(cardData.Value.IsFaceUp);

        // Actualizar sprite, rotacion, etc.
    }

    private string GetSuitFromIndex(int index)
    {
        string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
        return suits[Mathf.Clamp(index, 0, suits.Length - 1)];
    }
}
