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
    public SegmentPath SegmentPath;
    public Stack<Vector3> OverridePath = new Stack<Vector3>();
    public float CurrentLane = 1 * -0.2f;
    public int OnConnector = -1;
    public bool InIntersection = false;
    
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
    public int SegmentID;
    public int SegmentPathIndex;
    public Stack<int> Path;
}

[MessagePackObject(keyAsPropertyName: true)]
public class SegmentPath
{
    public int CurrentIndex;
    public int BottomIndex;
    public int TopIndex;
    public int Direction;
}