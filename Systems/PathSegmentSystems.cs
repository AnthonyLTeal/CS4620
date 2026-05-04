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
    
    public static void DestroySegmentConnector(int id)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        roadMesh.PathSegmentConnectors[id] = null;
    }

    public static void DestroySegment(int entity)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(entity);

        PathSegmentConnector endConnector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
        endConnector.SegmentEntities.Remove(entity);
        
        PathSegmentConnector frontConnector = roadMesh.PathSegmentConnectors[(int)segment.FrontConnector];
        frontConnector.SegmentEntities.Remove(entity);

        if (endConnector.SegmentEntities.Count == 0)
            DestroySegmentConnector((int)segment.EndConnector);
        
        if (frontConnector.SegmentEntities.Count == 0)
            DestroySegmentConnector((int)segment.FrontConnector);

        CalculateAllPaths();
        EntityManager.RemoveEntity(entity);
    }

    public static void CalculateAllPaths()
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();

        foreach (PathSegmentConnector connector in roadMesh.PathSegmentConnectors)
        {
            if (connector == null)
                continue;
            connector.DPath = CalculatePaths(connector, roadMesh);
            
            //Console.WriteLine($"Connector {connector.ID}: [{string.Join(", ", connector.DPath.Prevs)}]");
        }
    }

    public static void UpdateCongestionCost(float virtualDt)
    {
        List<int> entities = ComponentManager.GetComponent<PathSegment>();

        foreach (int entity in entities)
        {
            PathSegment pathSegment = ComponentManager.GetEntityComponent<PathSegment>(entity);

            pathSegment.AverageSquaredTimer += virtualDt;
            
            // if (pathSegment.EntitiesOnSegment.Count == 0 && MathF.Abs(pathSegment.CongestionCost) > 0.0001)
            // {
            //     continue;
            // }
            
            if (pathSegment.AverageSquaredTimer >= pathSegment.AverageSquaredTimeMax)
            {
                float squaredSum = 0;
                int nullCars = 0;

                foreach (int carEntity in pathSegment.EntitiesOnSegment)
                {
                    Car car = ComponentManager.GetEntityComponent<Car>(carEntity);
                    if (car == null)
                    {
                        //might be better to do a reverse for loop and forcibly remove them
                        nullCars += 1;
                        continue;
                    }

                    squaredSum += car.TimeOnSegment * car.TimeOnSegment;
                }

                int divisor = 1;

                if (pathSegment.EntitiesOnSegment.Count - nullCars > 0)
                    divisor = (pathSegment.EntitiesOnSegment.Count - nullCars);
                
                pathSegment.SquaredTimes.Enqueue(squaredSum/divisor);
                pathSegment.SquaredTimes.Dequeue();
                
                pathSegment.AverageSquaredTimer = 0;

                float totalSquaredTime = 0;
                foreach (int squaredTime in pathSegment.SquaredTimes)
                {
                    totalSquaredTime += squaredTime;
                }

                float sqrtTime = totalSquaredTime / 10 * 0.1f;
                float estimatedTime = GetEstimatedTimeToTravel(pathSegment);
                
                //Console.WriteLine("sqrtTime: " + sqrtTime);
                //Console.WriteLine("estimatedTime: " +  estimatedTime);

                if (sqrtTime > estimatedTime)
                    pathSegment.CongestionCost = sqrtTime - estimatedTime;
                else
                    pathSegment.CongestionCost = 0;

                //need a threshold or something
            }
        }
    }

    public static float GetEstimatedTimeToTravel(PathSegment segment)
    {
        float distance = 0;
        for (int i = 0; i < segment.Path.Length - 2; i++)
        {
            distance += Vector3.Distance(segment.Path[i], segment.Path[i + 1]);
        }
        
        SimulationSuper simulationSuper = EntityManager.GetGlobalComponent<SimulationSuper>();
        float speed = 2 * simulationSuper.SimSpeed;
        float estimatedTravelTime =  distance / speed;

        return estimatedTravelTime;
    }

    //Might want to change this in the future to a bucket based update to ensure we aren't
    //processing too many at a time since we are changing vertex color. This could get expensive
    //for now we use 3 congestion levels, 1, 2, 3
    public static void SetPathColor()
    {
        List<int> entities = ComponentManager.GetComponent<PathSegment>();
        foreach (int entity in entities)
        {
            int colorValue = 0;
            PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(entity);
            
            //Console.WriteLine($"Segment: {entity} | Entities On Segment: {segment.EntitiesOnSegment.Count}");
            
            if (segment.EntitiesOnSegment.Count >= 10)
            {
                if (segment.CurrentColorIndex != 3)
                    colorValue = 3;
                else continue;
            }
            else if (segment.EntitiesOnSegment.Count >= 5)
            {
                if (segment.CurrentColorIndex != 2)
                    colorValue = 2;
                else continue;
            }
            else
            {
                if (segment.CurrentColorIndex != 1)
                    colorValue = 1;
                else continue;
            }

            segment.CurrentColorIndex = colorValue;

            Color color = new Color(0.35f, 0.35f, 0.35f);

            if (colorValue == 2)
            {
                color = Color.Orange;
            }
            else if (colorValue == 3)
            {
                color = Color.Red;
            }

            RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();

            // Console.WriteLine($"Updating Segment: {entity}");
            // Console.WriteLine($"Color Index: {colorValue}");
            // Console.WriteLine($"Start Point: {segment.RibbonOffset}");
            // Console.WriteLine($"End Point: {segment.RibbonLength}");

            for (int i = segment.RibbonOffset; i < segment.RibbonOffset + segment.RibbonLength; i++)
            {
                roadMesh.RibbonMesh.UpdateVertexColor(i, color);
            }
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
        //Console.WriteLine("Distances: " + String.Join(", ", distances));
        //Console.WriteLine("Origin ID: " + origin.ID + " | Previous Path: " + String.Join(", ", prev));
        
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