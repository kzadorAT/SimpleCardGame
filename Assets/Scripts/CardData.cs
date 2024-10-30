using System;
using Unity.Netcode;

public struct CardData : INetworkSerializable, IEquatable<CardData>
{
    public int Value;
    public int SuitIndex;
    public bool IsFaceUp;
    public ulong OwnerId;
    public ulong NetworkObjectId;


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Value);
        serializer.SerializeValue(ref SuitIndex);
        serializer.SerializeValue(ref IsFaceUp);
        serializer.SerializeValue(ref OwnerId);
        serializer.SerializeValue(ref NetworkObjectId);
    }

    public bool Equals(CardData other)
    {
        return Value == other.Value &&
               SuitIndex == other.SuitIndex &&
               IsFaceUp == other.IsFaceUp &&
               OwnerId == other.OwnerId &&
               NetworkObjectId == other.NetworkObjectId;
    }

    public override bool Equals(object obj)
    {
        return obj is CardData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Value, SuitIndex, IsFaceUp, OwnerId, NetworkObjectId);
    }
}