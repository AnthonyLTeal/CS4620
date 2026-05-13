using System;
using System.Collections.Generic;
using CS4620IS.Components;
using MessagePack;
using PlanetaryExpansion;

namespace CS4620IS;

[MessagePackObject(keyAsPropertyName: true)]
public struct ReroutePath
{
    public int Origin;
    public int NextNode;
    public int DestinationNode;
    public float HeuristicDistance;
    public bool IsRerouting;
}

public class RerouteSystem
{
    public static ReroutePath Reroute(int depth, int origin, Destination destination)
    {
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        PathSegment targetSegment = ComponentManager.GetComponent<PathSegment>(destination.TargetSegmentID);

        int destOneId = (int)targetSegment.EndConnector;
        int destTwoId = (int)targetSegment.FrontConnector;

        float originHeuristicDistanceOne = CheckRoute(origin, destOneId); 
        float originHeuristicDistanceTwo = CheckRoute(origin, destTwoId);
        
        float bestEstimatedTime = originHeuristicDistanceTwo;
        PathSegmentConnector targetConnector = roadMesh.PathSegmentConnectors[destTwoId];

        if (originHeuristicDistanceTwo > originHeuristicDistanceOne)
        {
            bestEstimatedTime = originHeuristicDistanceOne;
            targetConnector = roadMesh.PathSegmentConnectors[destOneId];
        }

        float rawEstimatedTime = targetConnector.DPath.Distances[origin];

        //here we check if our bestDistance is any better than our raw distance, if not, it means there are no obstructions and we should continue to use that path
        if (Math.Abs(bestEstimatedTime - rawEstimatedTime) < 0.01f)
            return new ReroutePath();
        
        ReroutePath bestPath = new ReroutePath();

        UpdateBestPath(depth, origin, new List<int>(), destOneId, ref bestEstimatedTime, ref bestPath);
        UpdateBestPath(depth, origin, new List<int>(), destTwoId, ref bestEstimatedTime, ref bestPath);

        return bestPath;
    }
    
    public static void UpdateBestPath(int depth, int origin, List<int> previousNodes, int destinationNode, ref float bestDistance, ref ReroutePath bestPath)
    {
        previousNodes.Add(origin);

        List<int> adjacentConnectors = GetAdjacentConnectors(origin);
        
        //this section here detects if the current segment connector is an intersection and doesn't
        // if (adjacentConnectors.Count <= 2)
        // {
        //     for (int i = 0; i < adjacentConnectors.Count; i++)
        //     {
        //         if (adjacentConnectors[i] != origin && 
        //             adjacentConnectors[i] != destinationNode &&
        //             !previousNodes.Contains(adjacentConnectors[i]))
        //         {
        //             UpdateBestPath(depth, adjacentConnectors[i], previousNodes, destinationNode, ref bestDistance, ref bestPath);
        //         }
        //     }
        //     
        //     previousNodes.RemoveAt(previousNodes.Count - 1);
        //     return;
        // }
        
        float routeDistance = CheckRoute(destinationNode, origin);
        float prevDistance = 0;
        
        if (Math.Abs(bestDistance - (prevDistance + routeDistance)) < 0.01f && previousNodes.Count > 1)
        {
            bestPath = new ReroutePath()
            {
                Origin = origin,
                DestinationNode = destinationNode,
                NextNode = previousNodes[1],
                HeuristicDistance = routeDistance + prevDistance,
                IsRerouting = true
            };
        }

        if (depth == 0)
        {
            previousNodes.RemoveAt(previousNodes.Count - 1);
            return;
        }
        
        //RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        //DPath dPath = roadMesh.PathSegmentConnectors[destinationNode].DPath;
        
        foreach (int adjacentConnector in adjacentConnectors)
        {
            if (!previousNodes.Contains(adjacentConnector))
                UpdateBestPath(depth - 1, adjacentConnector, previousNodes, destinationNode, ref bestDistance, ref bestPath);
        }

        previousNodes.RemoveAt(previousNodes.Count - 1);
    }

    private static List<int> GetAdjacentConnectors(int connectorId)
    {
        List<int> adjacentConnectors = new List<int>();
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        PathSegmentConnector connector = roadMesh.PathSegmentConnectors[connectorId];

        foreach (int segmentEntity in connector.SegmentEntities)
        {
            PathSegment segment = ComponentManager.GetComponent<PathSegment>(segmentEntity);
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
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        PathSegmentConnector originConnector = roadMesh.PathSegmentConnectors[origin];
        
        float routeCost = originConnector.DPath.Distances[destination];
        
        while (destination != origin)
        {
            PathSegmentConnector intermediateConnector = roadMesh.PathSegmentConnectors[destination];
            foreach (int segmentEntity in intermediateConnector.SegmentEntities)
            {
                PathSegment segment = ComponentManager.GetComponent<PathSegment>(segmentEntity);
                if (((int)segment.EndConnector == origin || (int)segment.FrontConnector == origin) &&
                    ((int)segment.EndConnector == destination || (int)segment.FrontConnector == destination))
                {
                    if (segment.CongestionCost > segment.EstimatedTravelTime * 1)
                    {
                        routeCost += segment.CongestionCost;
                    }
                }
            }
            destination = originConnector.DPath.Prevs[destination];
        }
        return routeCost;
    }
}