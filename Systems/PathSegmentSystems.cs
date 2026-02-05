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
    
      public static DPath CalculatePaths(PathSegmentConnector origin, RoadMesh roadMesh)
    {
        List<PathSegmentConnector> pathSegmentConnectors = roadMesh.PathSegmentConnectors; //vertices
        //List<PathSegment> pathSegments = roadMesh.Segments; //edges

        PriorityQueue<PathSegmentConnector, int> minHeap = new PriorityQueue<PathSegmentConnector, int>();

        int[] distances = Enumerable.Repeat(-1, pathSegmentConnectors.Count).ToArray();
        int[] prev = Enumerable.Repeat(-1, pathSegmentConnectors.Count).ToArray();

        distances[origin.ID] = 0;
        prev[origin.ID] = origin.ID;
        //int depth = 0;
        
        minHeap.Enqueue(origin, 0);

        int pathSegmentCID = ComponentManager.GetComponentID<PathSegment>();
        
        while (minHeap.Count > 0)
        {
            var currentPoint = minHeap.Dequeue();
            foreach (var edgeID in currentPoint.SegmentEntities)
            {
                PathSegment edge = (PathSegment)EntityManager.EntityComponents[edgeID][pathSegmentCID];
                //PathSegment edge = pathSegments[edgeID];
                
                // Console.WriteLine($"Path ID: {edgeID}");
                // Console.WriteLine($"End Connector: {edge.EndConnector}");
                // Console.WriteLine($"Front Connector: {edge.FrontConnector}");

                 int? nextConnectorID = PathSegmentConnectorSystems.GetNextConnector(edge, currentPoint);
                 int distance = distances[currentPoint.ID] + edge.Weight;
                 if (distances[(int)nextConnectorID] == -1 || distance < distances[(int)nextConnectorID])
                 {
                     distances[(int)nextConnectorID] = distances[currentPoint.ID] + edge.Weight;
                     prev[(int)nextConnectorID] = currentPoint.ID;
                     minHeap.Enqueue(pathSegmentConnectors[(int)nextConnectorID], edge.Weight);
                }
            }
        }
        
        // Console.WriteLine("Origin ID: " + origin.ID);
        // Console.WriteLine("Distances: " + String.Join(", ", distances));
        // Console.WriteLine("Previous Path: " + String.Join(", ", prev));
        
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