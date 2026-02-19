using System;
using Microsoft.Xna.Framework;

namespace CS4620IS.Collision;

public class SphereIntersection
{
    //float squaredRadiusSum = (radiusA + radiusB) * (radiusA + radiusB);
    public static float SQUARED_RADIUS_SUM = .5f; //(1 + 1) * (1 + 1)
    
    public static bool StaticIntersection(Vector3 centerA, Vector3 centerB)
    {
        float distanceSquared = Vector3.DistanceSquared(centerA, centerB);
        //Console.WriteLine("Distance Squared: " + distanceSquared);
        return distanceSquared <= SQUARED_RADIUS_SUM * SQUARED_RADIUS_SUM;
    }
}