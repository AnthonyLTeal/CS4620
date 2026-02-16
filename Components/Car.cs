using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace CS4620IS.Components;


/// <summary>
/// Attach this and DrawableAsset to all cars
/// A group ID will tell us which group the 
/// </summary>
public class Car
{
    public int GroupID;
    public int ConnectedSegment;
    public int InitialPathIndex;
    public Vector3 Position;
    public Color Color;
    public Matrix Rotation;
    public float Scale = 0.25f;
    public List<Destination> Destinations;
    public SegmentPath SegmentPath;
    public Vector3 Offset;
    public Stack<Vector3> OverridePath = new Stack<Vector3>();
    public float CurrentLane = 1 * -0.2f;
}

public class Destination
{
    public int SegmentID;
    public int SegmentPathIndex;
    public Stack<int> Path;
}

public class SegmentPath
{
    public int CurrentIndex;
    public int BottomIndex;
    public int TopIndex;
    public int Direction;
}