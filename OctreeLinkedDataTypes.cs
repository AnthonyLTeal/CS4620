using CS4620IS.Collision;
using Microsoft.Xna.Framework;

namespace CS4620IS;

public class PathSegmentPointOctreeData
{
    public int PointIndex;
    public Vector3 Position;
    public bool Connector = false;
    //segment connector uses the EntityID as it's ID field
    public int EntityID;

    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is PathSegmentPointOctreeData))
            return false;
        if (Connector)
        {
            return EntityID == ((PathSegmentPointOctreeData)obj).EntityID;
        }
        return (EntityID == ((PathSegmentPointOctreeData)obj).EntityID &&
                PointIndex == ((PathSegmentPointOctreeData)obj).PointIndex);
    }
}

public class EntityOctreeDataPlaneSquare
{
    public int Entity { get; init; }
    public PlaneSquare PlaneSquare;

    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is EntityOctreeDataPlaneSquare))
            return false;
        return ((EntityOctreeDataPlaneSquare)obj).Entity == Entity;
    }

    public override int GetHashCode()
    {
        return Entity.GetHashCode();
    }

    public override string ToString()
    {
        return "OCTREE DATA " + Entity;
    }
}

public class EntityOctreeData
{
    public int Entity { get; init; }
    public BoundingOrientedBox BoundingOrientedBox;

    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is EntityOctreeData))
            return false;
        return ((EntityOctreeData)obj).Entity == Entity;
    }

    public override int GetHashCode()
    {
        return Entity.GetHashCode();
    }

    public override string ToString()
    {
        return "OCTREE DATA " + Entity;
    }
}