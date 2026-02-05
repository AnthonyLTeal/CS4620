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
    public Vector3 Position;
    public Color Color;
    public Matrix Rotation;
    public float Scale;
    public CubeMesh CubeMesh;
}