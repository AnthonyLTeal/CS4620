using System;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace CS4620IS.Collision;

public class SphereIntersection
{
    //float squaredRadiusSum = (radiusA + radiusB) * (radiusA + radiusB);
    public static float SQUARED_RADIUS_SUM = .25f; //(1 + 1) * (1 + 1)
    
    public static bool WillIntersect(Vector3 centerA, Vector3 centerB, Vector3 velocityA)
    {
        Vector3 toTarget = centerB - centerA;

        if (Vector3.Dot(toTarget, velocityA) <= 0f)
            return false;
        
        Vector3 nextA = centerA + velocityA;
       // Vector3 nextB = centerB + velocityB;
        
        float distanceSquared = Vector3.DistanceSquared(nextA, centerB);
        //Console.WriteLine("Distance Squared: " + distanceSquared);
        return distanceSquared <= SQUARED_RADIUS_SUM;
    }

    public static bool WillIntersect(Vector3 centerA, Vector3 centerB, float radius = 0.25f)
    {
        float distanceSquared = Vector3.DistanceSquared(centerA, centerB);
        //Console.WriteLine("Distance Squared: " + distanceSquared);
        return distanceSquared <= radius;
    }
}