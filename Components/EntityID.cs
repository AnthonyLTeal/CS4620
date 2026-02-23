using MessagePack;

namespace CS4620IS.Components;

[MessagePackObject(keyAsPropertyName: true)]
public struct EntityID
{
    public int ID;
}

[MessagePackObject(keyAsPropertyName: true)]
public struct EntityRef
{
    public int ID;
    public int Gen;
}