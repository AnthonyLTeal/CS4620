using System;
using System.Collections.Generic;
using CS4620IS.Components;
using PlanetaryExpansion;

namespace CS4620IS;

public struct ReroutePath
{
    public int Origin; //might be able to remove either Origin or PreviousNode, they may really contain the same information
    public int PreviousNode;
    public int IntermediaryNode;
    public int DestinationNode;
    public float HeuristicDistance;
}

public class RerouteSystem
{
    public static ReroutePath? Reroute(int depth, int origin, Destination destination)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegment targetSegment = ComponentManager.GetEntityComponent<PathSegment>(destination.TargetSegmentID);

        int destOneEntity = (int)targetSegment.EndConnector;
        int destTwoEntity = (int)targetSegment.FrontConnector;
        
        //we should CheckRoute on the destination
        float bestDistance = 9999;
        
        ReroutePath? bestPath = null;

        UpdateBestPath(depth, origin, origin, destOneEntity, ref bestDistance, ref bestPath, new List<int>());
        UpdateBestPath(depth, origin, origin, destTwoEntity, ref bestDistance, ref bestPath, new List<int>());

        return bestPath;
    }
    
    public static void UpdateBestPath(int depth, int origin, int previousNode, int destinationNode, ref float bestDistance, ref ReroutePath? bestPath, List<int> ignoreNodes)
    {
        float routeDistance = CheckRoute(destinationNode, previousNode);
        if (Math.Abs(bestDistance - routeDistance) < 0.01f)
        {
            //create new ReroutePath
        }

        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        DPath dPath = roadMesh.PathSegmentConnectors[origin].DPath;

        if (depth != 0)
        {
            if ()
        }
    }
    
    /// <summary>
    /// Returns the distance with all heuristics added 
    /// </summary>
    public static float CheckRoute(int destinationNode, int fromNode)
    {
        return 0;
    }
}