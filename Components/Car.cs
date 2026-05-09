using System;
using System.Collections.Generic;
using MessagePack;
using Microsoft.Xna.Framework;

namespace CS4620IS.Components;


/// <summary>
/// Attach this and DrawableAsset to all cars
/// A group ID will tell us which group the 
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public class Car
{
    public int connectedSegmentId;
    public PathSegment ConnectedSegment;
    public int PreviousSegment;
    public int InitialPathIndex;
    public Vector3 Position;
    public Color Color;
    public float Scale; 
    //public DestinationPointer DestinationPointer;
    public Queue<Destination> Destinations = new Queue<Destination>();
    public List<String> Log = new List<string>();
    public CarPath CarPath;
    public Stack<Vector3> OverridePath = new Stack<Vector3>();
    public float CurrentLane;
    public int OnConnector;
    public bool IgnoreYellow;
    public ReroutePath ReroutePath;
    public bool IsReroute;
    public float TimeOnSegment;
    public float WaitTimer;
    public float StuckCooldown;
    public bool InIntersection;
    public bool Alive;
    
    [IgnoreMember]
    public float Rotation;
    
    // public static readonly Car Default =  new Car
    // {
    //     CurrentLane = 1 * -0.2f,
    //     Scale = 0.25f,
    //     OnConnector = -1,
    //     IgnoreYellow = false,
    //     
    // };
    
    //public List<String> Log = new List<String>();
    
    [IgnoreMember]
    public int ConnectedSegmentId
    {
        get => connectedSegmentId;
        set
        {
            if (value == 0) 
            {
                Console.WriteLine("ERROR: dunno");
            }
            else
            {
                connectedSegmentId = value;
            }
        }
    }
}

[MessagePackObject(keyAsPropertyName: true)]
public struct DestinationPointer
{
    public int Start;
    public int Count;
    public int Current;
    
    public DestinationPointer(int start, int count)
    {
        Start = start;
        Count = count;
        Current = 0;
    }
}

[MessagePackObject(keyAsPropertyName: true)]
public struct Destination
{
    public int TargetSegmentID;
    public int TargetPathIndex;
}

[MessagePackObject(keyAsPropertyName: true)]
public struct CarPath
{
    public int CurrentIndex;
    public int Direction;
    public int SegmentSize;
    public int LastConnectorID;
    public int NextConnectorID;

    public CarPath(int currentIndex, int direction, int segmentSize,  int lastConnectorID = -1, int nextConnectorID = -1)
    {
        CurrentIndex = currentIndex;
        Direction = direction;
        SegmentSize = segmentSize;
        LastConnectorID = lastConnectorID;
        NextConnectorID = nextConnectorID;
    }
}