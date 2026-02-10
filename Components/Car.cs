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
    public int LastPathIndex;
    public Vector3 Position;
    public Color Color;
    public Matrix Rotation;
    public float Scale;
    public List<Destination> Destinations;
}

public class Destination
{
    public int SegmentID;
    public int SegmentPathIndex;
    public Stack<int> Path;
}