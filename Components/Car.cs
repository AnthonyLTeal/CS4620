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
    public int GroupID;
    public int connectedSegment;
    public int PreviousSegment;
    public int InitialPathIndex;
    public Vector3 Position;
    public Color Color;
    public float Scale = 0.25f;
    public List<Destination> Destinations;
    public CarPath SegmentPath;
    public Stack<Vector3> OverridePath = new Stack<Vector3>();
    public float CurrentLane = 1 * -0.2f;
    public int OnConnector = -1;
    public bool InIntersection = false;
    //graph things
    public double AliveTime = 0.0;
    public double WaitTime = 0.0;
    //public double SignDelayTime;
    //public double ReroutingDelayTime;
    //------------------------------------------
    public List<String> Log = new List<String>();
    
    [IgnoreMember]
    public int ConnectedSegment
    {
        get => connectedSegment;
        set
        {
            if (value == 0)
            {
                Console.WriteLine("ERROR: WTF");
            }
            else
            {
                connectedSegment = value;
            }
        }
    }

    [IgnoreMember]
    public Matrix Rotation;
}

[MessagePackObject(keyAsPropertyName: true)]
public class Destination
{
    public int TargetSegmentID;
    public int TargetPathIndex;
}

[MessagePackObject(keyAsPropertyName: true)]
public class CarPath
{
    public int CurrentIndex;
    public int Direction;
    public int SegmentSize;
    public int LastConnectorID = -1;
    public int NextConnectorID = -1;
}