using System.Collections.Generic;
using MessagePack;

namespace CS4620IS;

[MessagePackObject(keyAsPropertyName: true)]
public class SavedComponent
{
    public int EntityID { get; set; }           // entity this component belongs to
    public string TypeName { get; set; }        // assembly-qualified name
    public byte[] ComponentsData { get; set; }            // serialized component
}