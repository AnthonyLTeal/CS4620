using System.Collections.Generic;
using System.IO;
using System.Threading;
using CS4620IS.Components;

namespace CS4620IS;

public class BasicPathFinding
{
    public static void Basic()
    {
        // int pathSegmentComponentID = ComponentManager.GetComponentID<PathSegment>(); 
        List<int> pathSegments = ComponentManager.GetComponent<PathSegment>();

        foreach (var entity in pathSegments)
        {
            PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(entity);
            //CalculatePerfectPath(segment)
        }
    }
}