using System;
using Microsoft.Xna.Framework;

namespace CS4620IS.Collision;

public struct PlaneSquare
{
    public Plane Plane;
    public float Size;
    public Vector3 PlaneRight;
    public Vector3 PlaneUp;
    public Vector3 P1;
    public Vector3 P2;
    public Vector3 P3;
    public Vector3 P4;
    public Vector3 Center;
    // private Vector3 z_axis_proj;
    // private Vector3 y_axis_proj;
    // private Vector3 x_axis_proj;
    private BoundingSphere earlySphere;

    public PlaneSquare(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        Center = (p1 + p2 + p3 + p4) / 4f;
        PlaneRight = Vector3.Normalize(p2 - p1);
        PlaneUp = Vector3.Normalize(p3 - p1);
        Plane = new Plane(Center, Vector3.Cross(PlaneRight, PlaneUp));
        Size = Vector3.Distance(p1, p2);
        P1 = p1;
        P2 = p2;
        P3 = p3;
        P4 = p4;
        
        // Vector3 old_x_axis = new Vector3(1, 0, 0);
        // z_axis_proj = Plane.Normal;
        // y_axis_proj = Vector3.Cross(old_x_axis, z_axis_proj);
        // y_axis_proj.Normalize();
        // x_axis_proj = Vector3.Cross(z_axis_proj, y_axis_proj);
        // x_axis_proj.Normalize();

        earlySphere = new BoundingSphere(Center, Vector3.Distance(p1, p4));
    }
    
    /// <summary>
    /// Calculates the 2d projected point of intersection on a rectangle (specific width and height)
    /// of a plane and determines if a collision happened.
    /// </summary>
    /// <returns>Vector2? primarily used as boolean to see if collision happened within the bounds of the rectangle that exists on the plane</returns>
    public Vector2? PointPlaneProjection(Vector3 point)
    {
        // Orthonormalize just in case
        Vector3 x_axis = Vector3.Normalize(PlaneRight);
        Vector3 y_axis = Vector3.Normalize(PlaneUp);
        Vector3 z_axis = Vector3.Normalize(Plane.Normal);

        // Ensure the axes are truly orthogonal
        y_axis = Vector3.Normalize(Vector3.Cross(z_axis, x_axis));
        x_axis = Vector3.Normalize(Vector3.Cross(y_axis, z_axis));

        // Project into local coordinates
        Vector3 local = point - Center;

        float x = Vector3.Dot(local, x_axis);
        float y = Vector3.Dot(local, y_axis);
        float z = Vector3.Dot(local, z_axis);

        // Make sure the point lies *on* the plane
        if (Math.Abs(z) > 1e-4f)
            return null;

        // Include a small tolerance for edge precision
        float halfW = Size / 2f;
        float halfH = Size / 2f;
        const float edgeTol = 1e-4f;

        if (x >= -halfW - edgeTol && x <= halfW + edgeTol &&
            y >= -halfH - edgeTol && y <= halfH + edgeTol)
            return new Vector2(x, y);

        return null;
    }

    public float? Intersects(ref Ray ray)
    {
        //we can escape early if there isn't a hit on the sphere
        if (ray.Intersects(earlySphere) is null)
            return null;
        
        float? collisionDistance = ray.Intersects(Plane);

        if (collisionDistance != null)
        {
            Vector3 point = ray.Position + ray.Direction * (float)collisionDistance;
            //Vector2? squareCollision = PointPlaneProjection(point, Center, Size, Size);
            //Vector2? squareCollision = PointPlaneProjection(point, Center, Plane.Normal, Size, Size);
            Vector2? squareCollision = PointPlaneProjection(point);
            if (squareCollision != null)
            {
                //Console.WriteLine("Ray Intersected Plane Square");
                return collisionDistance;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if a bounding box intersects or contains a plane square. This should only be used if it is
    /// guaranteed that the boundingbox is larger than the planesquare
    /// </summary>
    public bool IntersectsCheap(ref BoundingBox boundingBox)
    {
        Vector3[] corners = { P1, P2, P3, P4 };
        foreach (var point in corners)
            if (boundingBox.Contains(point) != ContainmentType.Disjoint)
                return true;

        // 3. Any box corner inside square plane region?
        Vector3 boxCenter = (boundingBox.Min + boundingBox.Max) * .5f;
        foreach (var point in boundingBox.GetCorners())
        {
            Vector3 rayDir = Vector3.Normalize(point - boxCenter);
            Ray ray = new Ray(boxCenter, rayDir);
            if (Intersects(ref ray) != null)
                return true;
        }

        return false;
    }

    public bool Intersects(ref BoundingBox boundingBox)
    {
        Vector3[] corners = { P1, P2, P3, P4 };
        foreach (var point in corners)
            if (boundingBox.Contains(point) != ContainmentType.Disjoint)
                return true;

        // 2. Any square edge intersect box?
        for (int i = 0; i < 4; i++)
        {
            Vector3 p1 = corners[i];
            Vector3 p2 = corners[(i + 1) % 4];
            Vector3 dir = Vector3.Normalize(p2 - p1);
            float len = Vector3.Distance(p1, p2);
            Ray edgeRay = new Ray(p1, dir);
            float? dist = boundingBox.Intersects(edgeRay);
            if (dist.HasValue && dist.Value >= 0f && dist.Value <= len)
                return true;
        }

        // 3. Any box corner inside square plane region?
        foreach (var point in boundingBox.GetCorners())
            if(PointPlaneProjection(point) != null)
                return true;
            // if(PointPlaneProjection(point, Center, Plane.Normal, Size, Size) != null)
            //if (PointPlaneProjection(point, Center, Size, Size) != null)

        return false;
    }
}