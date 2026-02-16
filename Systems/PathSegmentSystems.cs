using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
// using Myra.Graphics2D.UI;
using CS4620IS.Components;
using PlanetaryExpansion;

namespace CS4620IS;

public enum OneWay
{
    None = 0,
    Right = 1,
    Left = 2
}

public class PathSegmentSystems
{    
    public static VertexPositionColor[] GenerateRoadOutline(Vector3[] path, bool isLaneRuler)
    {
        Color color = isLaneRuler == true ? Color.Red : Color.White;
        VertexPositionColor[] vertexPositionColor = new VertexPositionColor[path.Length];
        for (int i = 0; i < path.Length; i++)
        {
            vertexPositionColor[i] = new VertexPositionColor(path[i], color);
            //points[0] = new VertexPositionColor(p1, color);
        }

        return vertexPositionColor;
    }

    public static void CalculateAllPaths()
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();

        foreach (PathSegmentConnector connector in roadMesh.PathSegmentConnectors)
        {
            connector.DPath = CalculatePaths(connector, roadMesh);
        }
    }
    
    public static DPath CalculatePaths(PathSegmentConnector origin, RoadMesh roadMesh)
    {
        List<PathSegmentConnector> pathSegmentConnectors = roadMesh.PathSegmentConnectors; //vertices
        //List<PathSegment> pathSegments = roadMesh.Segments; //edges

        int[] distances = Enumerable.Repeat(-1, pathSegmentConnectors.Count).ToArray();
        int[] prev = Enumerable.Repeat(-1, pathSegmentConnectors.Count).ToArray();

        distances[origin.ID] = 0;
        prev[origin.ID] = origin.ID;
        //int depth = 0;
        int pathSegmentCID = ComponentManager.GetComponentID<PathSegment>();

        PriorityQueue<(int node, int dist), int> minHeap = new();

        minHeap.Enqueue((origin.ID, 0), 0);

        while (minHeap.Count > 0)
        {
            var (currentID, queuedDist) = minHeap.Dequeue();

            // THIS is the missing rule
            if (queuedDist != distances[currentID])
                continue;

            var currentPoint = roadMesh.PathSegmentConnectors[currentID];

            foreach (var edgeID in currentPoint.SegmentEntities)
            {
                PathSegment edge = (PathSegment)EntityManager.EntityComponents[edgeID][pathSegmentCID];
                int nextID = PathSegmentConnectorSystems.GetNextConnector(edge, currentPoint)!.Value;

                int newDist = queuedDist + edge.Weight;

                if (distances[nextID] == -1 || newDist < distances[nextID])
                {
                    distances[nextID] = newDist;
                    prev[nextID] = currentID;
                    minHeap.Enqueue((nextID, newDist), newDist);
                }
            }
        }
        
        //Console.WriteLine();
        Console.WriteLine("Distances: " + String.Join(", ", distances));
        Console.WriteLine("Origin ID: " + origin.ID + " | Previous Path: " + String.Join(", ", prev));
        
        return new DPath(distances, prev);
    }

    // public static void DebugDraw()
    // {
    //     effect.View = viewMatrix;
    //     effect.Projection = projectionMatrix;
    //     effect.DiffuseColor = new Vector3(1f, 1f, 1f);
    //     effect.CurrentTechnique.Passes[0].Apply();
    //
    //     for (int i = 0; i < Points.Length - 1; i++)
    //     {
    //         graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{Points[i], Points[i + 1]}, 0, 1);
    //     }
    //     //
    //     // effect.DiffuseColor = new Vector3(0, 0, 1f);
    //     // effect.CurrentTechnique.Passes[0].Apply();
    //     // for (int i = 0; i < connectedSegments.Count; i++)
    //     // {
    //     //     PathSegment connectedSegment = connectedSegments[i];
    //     //     int connectedFromIndex = connectedIndex[i * 2];
    //     //     int connectedToIndex = connectedIndex[i * 2 + 1];
    //     //
    //     //     if (connectedFromIndex > 0)
    //     //     {
    //     //         graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{points[connectedIndex[i * 2]], points[connectedIndex[i * 2] - 1]}, 0, 1);
    //     //     }
    //     //     
    //     //     if (connectedFromIndex < Path.Length - 1)
    //     //     {
    //     //         graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{points[connectedIndex[i * 2]], points[connectedIndex[i * 2] + 1]}, 0, 1);
    //     //     }
    //     //     //graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{connectedSegments[i].points[connectedIndex[i * 2]], points[connectedIndex[i * 2 + 1]]}, 0, 1);
    //     //     //graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{points[connectedIndex[i * 2]], points[connectedIndex[i * 2] + 1]}, 0, 1);
    //     // }
    // }

    // public void AddConnectedSegment(PathSegment segment, int connectedFromIndex, int connectedToIndex)
    // {
    //     //connectedSegments.Add(segment);
    //     connectedIndex.Add(connectedFromIndex);
    //     connectedIndex.Add(connectedToIndex);
    // }
}