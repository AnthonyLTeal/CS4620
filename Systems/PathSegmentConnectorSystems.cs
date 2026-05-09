using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CS4620IS.Components;
using PlanetaryExpansion;

namespace CS4620IS;

public enum SegmentConnectorIndex
{
    First,
    Last
}

public class PathSegmentConnectorSystems
{
    public static void AddSegmentToSegmentConnector(PathSegmentConnector connector, PathSegment segment, SegmentConnectorIndex segmentConnectorIndex)
    {
        int index = segmentConnectorIndex == SegmentConnectorIndex.First ? 0 : segment.Path.Length - 1;

        connector.SegmentEntities.Add(segment.EntityID);
        connector.PointIDs.Add(index);
        connector.DebugPoints.Add(new VertexPositionColor(segment.Path[index], Color.Blue));

        if (index == 0)
        {
            segment.FrontConnector = connector.ID;
        }
        else
        {
            segment.EndConnector = connector.ID;
        }
        
        GenerateStopSign(connector);
    }

    //TODO test this, it might not work right
    public static void RemoveSegmentFromConnector(PathSegmentConnector connector, int segmentEntity, int pointIndex)
    {
        for (int i = 0; i < connector.SegmentEntities.Count; i++)
        { 
            //Console.WriteLine($"Entity: {connector.SegmentEntities[i]} Path Point: {connector.PointIDs[i]} | EntityToRemove: {segmentEntity} EntityPointIndex: {pointIndex}");
            if (connector.SegmentEntities[i] == segmentEntity && connector.PointIDs[i] == pointIndex)
            {
                connector.SegmentEntities.RemoveAt(i);
                connector.PointIDs.RemoveAt(i);
                connector.DebugPoints.RemoveAt(i);
                break;
            }
        }
    }

    public static int? GetNextConnector(PathSegment segment, PathSegmentConnector fromConnector)
    {
        if (segment.EndConnector == fromConnector.ID)
        {
            return segment.FrontConnector;
        }

        return segment.EndConnector;
    }
    
    private static void DebugWriteSegmentPathTrace(List<PathSegmentConnector> connectors, PathSegmentConnector connector, List<int> traversedConnectorsIds)
    {
        if (traversedConnectorsIds.Contains(connector.ID))
            return;
        traversedConnectorsIds.Add(connector.ID);
        
        for (int i = 0; i < connector.SegmentEntities.Count; i++)
        {
            //we will do this to make sure we don't end up in an endless loop for now
            
            ref  PathSegment segment = ref ComponentManager.GetComponent<PathSegment>(connector.SegmentEntities[i]);
            
            int? nextconnectorId = GetNextConnector(segment, connector);
            if (nextconnectorId == null)
            {
                throw new InvalidOperationException("nextConnectorId is null and should never be null");
            }
            PathSegmentConnector nextconnector = connectors[(int)nextconnectorId];
            
            //Console.WriteLine($"Path: {connector.ID} <-> {nextconnector.ID}");
            
            DebugWriteSegmentPathTrace(connectors, nextconnector, traversedConnectorsIds);
        }
    }

    public static void DebugWriteSegmentPathTrace(List<PathSegmentConnector> connectors, PathSegmentConnector connector)
    {
        for (int i = 0; i < connectors.Count; i++)
        {
            PathSegmentConnector _connector = connectors[i];
            Console.WriteLine($"Segments connected to connector: {_connector.ID}");
        
            for (int j = 0; j < _connector.SegmentEntities.Count; j++)
            {
                Console.WriteLine(_connector.SegmentEntities[j]);
            }
            
            Console.WriteLine();
        }
        List<int> traversedConnectorIds = new List<int>();
        DebugWriteSegmentPathTrace(connectors, connector, traversedConnectorIds);
    }
    
    public static void AddConnector(PathSegmentConnector connector)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        roadMesh.PathSegmentConnectors.Add(connector);
    }

    public static void GenerateStopSign(PathSegmentConnector connector)
    {
        if (connector.SegmentEntities.Count < 3)
            return;
        
        connector.StopSignLocations.Clear();

        foreach (int segmentEntity in connector.SegmentEntities)
        {
            PathSegment pathSegment = ComponentManager.GetEntityComponent<PathSegment>(segmentEntity);
            Vector3 signPosition = pathSegment.Path[pathSegment.Path.Length - 1];
            
            if (connector.ID == (int)pathSegment.FrontConnector)
                signPosition = pathSegment.Path[0];
            
            connector.StopSignLocations.Add(signPosition);
        }
    }
}