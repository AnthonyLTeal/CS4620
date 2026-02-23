using System;
using System.Collections.Generic;
using CS4620IS.Components;
using MessagePack;

namespace CS4620IS;

[MessagePackObject(keyAsPropertyName: true)]
public class WorldState
{
    public List<int[]> Entities { get; set; }
    public List<SavedComponent> EntityComponents { get; set; }
    public bool Reserved { get; set; }
    public List<int> DeadEntities { get; set; }
    public int LastAddedEntity { get; set; }
    public List<int> EntityGen { get; set; }

    //public Dictionary<Type, List<int>> ComponentRegistery { get; set; }
    //public Dictionary<Type, int> ComponentIDs { get; set; }
    //public int TotalComponents;
    public List<PathSegmentConnector> PathSegmentConnectors { get; set; }
    //public List<SomeOtherComponent> OtherComponents { get; set; }
    //public int TickCount { get; set; } // Example singleton
}