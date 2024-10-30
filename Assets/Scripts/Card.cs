using Unity.Netcode;

public class Card : INetworkSerializable
{
    private int _value;
    public int Value
    {
        get => _value;
        set => _value = value;
    }

    private string _suit;
    public string Suit
    {
        get => _suit;
        set => _suit = value;
    }

    private bool _isFaceUp;
    public bool IsFaceUp
    {
        get => _isFaceUp;
        set => _isFaceUp = value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _value);
        if (serializer.IsReader)
            _suit = string.Empty;
        serializer.SerializeValue(ref _suit);
        serializer.SerializeValue(ref _isFaceUp);
    }
} 