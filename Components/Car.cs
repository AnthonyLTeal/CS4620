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
    public int InitialPathIndex;
    public Vector3 Position;
    public Color Color;
    public float Scale = 0.25f;
    public List<Destination> Destinations;
    public CarPath CarPath;
    public Stack<Vector3> OverridePath = new Stack<Vector3>();
    public float CurrentLane = 1 * -0.2f;
    public int OnConnector = -1;
    
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
    public int SegmentID;
    public int SegmentPathIndex;
    public Stack<int> Path;
}

[MessagePackObject(keyAsPropertyName: true)]
public class CarPath
{
    public int CurrentIndex;
    public int Direction;
    public Queue<Vector3> Positions;
    public int PathSize;
}