using System;
using System.Collections.Generic;
using CS4620IS.Components;
using PlanetaryExpansion;

namespace CS4620IS;

public struct ReroutePath
{
    public int Origin; //might be able to remove either Origin or PreviousNode, they may really contain the same information
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

        int destOneId = (int)targetSegment.EndConnector;
        int destTwoId = (int)targetSegment.FrontConnector;

        float originHeuristicDistanceOne = CheckRoute(origin, destOneId); 
        float originHeuristicDistanceTwo = CheckRoute(origin, destTwoId);
        
        //we should CheckRoute on the destination
        float bestDistance = originHeuristicDistanceTwo;
        PathSegmentConnector targetConnector = roadMesh.PathSegmentConnectors[destTwoId];

        if (originHeuristicDistanceTwo > originHeuristicDistanceOne)
        {
            bestDistance = originHeuristicDistanceOne;
            targetConnector = roadMesh.PathSegmentConnectors[destOneId];
        }

        int rawDistance = targetConnector.DPath.Distances[origin];

        if (Math.Abs(bestDistance - rawDistance) < 0.01f)
            return null;
        
        ReroutePath? bestPath = null;

        UpdateBestPath(depth, origin, origin, destOneId, ref bestDistance, ref bestPath, new List<int>());
        UpdateBestPath(depth, origin, origin, destTwoId, ref bestDistance, ref bestPath, new List<int>());

        return bestPath;
    }
    
    public static void UpdateBestPath(int depth, int origin, int previousNode, int destinationNode, ref float bestDistance, ref ReroutePath? bestPath, List<int> ignoreNodes)
    {
        ignoreNodes.Add(destinationNode);

        List<int> adjacentConnectors = GetAdjacentConnectors(previousNode);
        
        if (adjacentConnectors.Count == 2)
        {
            if (adjacentConnectors[0] != origin || 
                adjacentConnectors[0] != destinationNode ||
                !ignoreNodes.Contains(adjacentConnectors[0]))
            {
                UpdateBestPath(depth, origin, adjacentConnectors[0], destinationNode, ref bestDistance, ref bestPath, adjacentConnectors);
            }
            
            if (adjacentConnectors[1] != origin || 
                adjacentConnectors[1] != destinationNode ||
                !ignoreNodes.Contains(adjacentConnectors[1]))
            {
                UpdateBestPath(depth, origin, adjacentConnectors[0], destinationNode, ref bestDistance, ref bestPath, adjacentConnectors);
            }

            return;
        }
        
        float routeDistance = CheckRoute(destinationNode, previousNode);
        float prevDistance = 0;

        if (origin != previousNode)
            prevDistance = CheckRoute(origin, previousNode);
        
        if (Math.Abs(bestDistance - (prevDistance + routeDistance)) < 0.01f)
        {
            bestPath = new ReroutePath()
            {
                Origin = origin,
                DestinationNode = destinationNode,
                IntermediaryNode = previousNode,
                HeuristicDistance = routeDistance + prevDistance
            };
        }

        if (depth == 0)
            return;
        
        //RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        //DPath dPath = roadMesh.PathSegmentConnectors[destinationNode].DPath;
        
        foreach (int adjacentConnector in adjacentConnectors)
        {
            if (!ignoreNodes.Contains(adjacentConnector))
                UpdateBestPath(depth - 1, origin, adjacentConnector, destinationNode, ref bestDistance, ref bestPath, adjacentConnectors);
        }
    }

    private static List<int> GetAdjacentConnectors(int connectorId)
    {
        List<int> adjacentConnectors = new List<int>();
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegmentConnector connector = roadMesh.PathSegmentConnectors[connectorId];

        foreach (int segmentEntity in connector.SegmentEntities)
        {
            PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(segmentEntity);
            if ((int)segment.EndConnector == connectorId)
                adjacentConnectors.Add((int)segment.FrontConnector);
            else
                adjacentConnectors.Add((int)segment.EndConnector);
        }

        return adjacentConnectors;
    }
    
    /// <summary>
    /// Returns the distance with all heuristics added 
    /// </summary>
    public static float CheckRoute(int origin, int destination)
    {
        return 0;
    }
}