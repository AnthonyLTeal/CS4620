using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Numerics;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlanetaryExpansion;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace CS4620IS;

public class CarSystems
{
    private const float UPDATE_TIME = 1000f;
    private static float _carTimer = 0;

    public static void BasicBehavior(GameTime gameTime)
    {
         List<int> entities = ComponentManager.GetComponent<Car>();
         
         foreach (int entity in entities)
         {
             Car car = ComponentManager.GetEntityComponent<Car>(entity);

             if (car.Destinations.Count < 1)
             {
                 EntityManager.RemoveEntity(entity);
                 return;
             }

             PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
             MoveCar(car, connectedSegment.Speed * (float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.01f);

         }
    }

    private static Vector3 GetPathVertex(int index, PathSegment pathSegment)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        if (index == 0)
        {
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)pathSegment.FrontConnector];
            return connector.Position;
        } 
        if (index >= pathSegment.Path.Length + 1)
        {
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)pathSegment.EndConnector];
            return connector.Position;
        }

        return pathSegment.Path[index - 1];
    }

    //TODO need to add collision check here for cars so they don't hit/pass through each other, especially at intersections
    private static void MoveCar(Car car, float velocity)
    {
        while (velocity > 0)
        {
            PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
            //Console.WriteLine("Current Index: " + car.SegmentPath.CurrentIndex);
            Vector3 nextPoint = GetPathVertex(car.SegmentPath.CurrentIndex + car.SegmentPath.Direction, connectedSegment);
        
            Vector3 direction = Vector3.Normalize(nextPoint - car.Position);
            float distanceTo = Vector3.Distance(nextPoint, car.Position);
            float remaining = velocity - distanceTo;
            if (remaining <= 0)
            {
                car.Position += direction * velocity;
                car.Rotation = Matrix.CreateWorld(Vector3.Zero, direction, Vector3.Up);
                //TODO need to update rotation here
                return;
            }

            velocity = remaining;
            
            car.SegmentPath.CurrentIndex += car.SegmentPath.Direction;
            car.Position = nextPoint;

            if (car.SegmentPath.CurrentIndex == car.SegmentPath.TopIndex || car.SegmentPath.CurrentIndex == car.SegmentPath.BottomIndex)
            {
                if (car.Destinations[0].Path.Count == 0)
                {
                    car.Destinations.RemoveAt(0);
                    return;
                }
                
                //get new segment
                int lastConnectorID = car.Destinations[0].Path.Pop();
                //Console.WriteLine("POPPING FROM PATH: " + lastConnectorID);
                //Console.WriteLine("REMAINING: " + car.Destinations[0].Path.ToArray());
                car.ConnectedSegment = GetNextSegment(car, lastConnectorID);
                car.SegmentPath = BuildSegmentPath(car, car.Destinations[0], lastConnectorID);
            }
        }
    }

    private static int GetNextSegment(Car car, int lastConnectorID)
    {
        if (car.Destinations[0].Path.Count == 0)
            return car.Destinations[0].SegmentID;
        
        PathSegmentConnector connector = EntityManager.GetGlobalComponent<RoadMesh>().PathSegmentConnectors[lastConnectorID];
        int nextConnectorID = car.Destinations[0].Path.Peek();
        foreach (int segmentEntity in connector.SegmentEntities)
        {
            PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(segmentEntity);
            if ((segment.EndConnector == nextConnectorID && segment.FrontConnector == lastConnectorID) ||
                (segment.EndConnector == lastConnectorID && segment.FrontConnector == nextConnectorID))
            {
                return segmentEntity;
            }
        }

        return 0; //should never get here so might be good to throw an error
        //throw new InvalidOperationException($"No segment connects connectors {lastConnectorID} -> {nextConnectorID}");
    }

    /// <summary>
    /// Returns true if the nextConnectorID is an end connector or false if it's a front connector on the given segment
    /// </summary>
    private static bool IsDestinationEnd(PathSegment pathSegment, int nextConnectorID)
    {
        if (pathSegment.EndConnector == nextConnectorID)
            return true;
        return false;
    }

    private static SegmentPath BuildSegmentPath(Car car,  Destination destination, int? lastConnectorID = null)
    {
        SegmentPath segmentPath = new SegmentPath();
        PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        
        //we should only need to change i1 or i2 (boundaries) when we are going to a location on a segment
        
        int i1 = 0;
        int i2 = connectedSegment.TotalPathLength - 1; //need to treat this like an index for an array
        int traverseDir = 1;
        
        if (lastConnectorID != null) //from connector to connecter
        {
            if (connectedSegment.FrontConnector == lastConnectorID)
            {
                traverseDir = 1;
                segmentPath.CurrentIndex = 0;
            }
            if (connectedSegment.EndConnector == lastConnectorID)
            {
                traverseDir = -1;
                segmentPath.CurrentIndex = connectedSegment.TotalPathLength - 1; //need to treat this like an index for an array
            }
        }
        
        if (lastConnectorID == null)  //from segment
        {
            segmentPath.CurrentIndex = car.InitialPathIndex;
            if (destination.Path.Count > 0) //from segment to connector
            {
                int nextDest = car.Destinations[0].Path.Peek();
                if (nextDest == connectedSegment.EndConnector)
                    traverseDir = 1;
                else
                    traverseDir = -1;
            }
            else //from segment to segment
            {
                if (destination.SegmentPathIndex > segmentPath.CurrentIndex)
                    traverseDir = 1;
                else
                    traverseDir = -1;
            }
        }
        
        if (destination.Path.Count == 0) //from point to segment
        {
            if (traverseDir == 1)
            {
                i2 = destination.SegmentPathIndex;
            }
            else
            {
                i1 = destination.SegmentPathIndex;
            }
        }

        segmentPath.Direction = traverseDir;
        segmentPath.BottomIndex = i1;
        segmentPath.TopIndex = i2;
        
        //Console.WriteLine("Direction: " + traverseDir);
        //Console.WriteLine("BottomIndex: " + i1);
        //Console.WriteLine("TopIndex: " + i2);
        //Console.WriteLine("CurrentIndex: " + segmentPath.CurrentIndex);
        
        return segmentPath;
    }

    private static Random random = new Random();

    /// <summary>
    /// Gets a random path point
    /// </summary>
    /// <returns>(int SegmentEntityID, int SegmentPathPointIndex)</returns>
    public static (int, int) GetRandomPathPoint()
    {
        List<int> segments = ComponentManager.GetComponent<PathSegment>();
        int randomSegment = random.Next(segments.Count);
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(segments[randomSegment]);
        int randomPathPoint = random.Next(segment.Path.Length);
        return (segment.EntityID, randomPathPoint);
    }

    public static Stack<int> GetShortestPath(PathSegment currentSegment, Destination destination)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegment destSegment = ComponentManager.GetEntityComponent<PathSegment>(destination.SegmentID);

        //just added these lines for readability
        int destFront = (int)destSegment.FrontConnector;
        int destEnd = (int)destSegment.EndConnector;
        int front = (int)currentSegment.FrontConnector;
        int end = (int)currentSegment.EndConnector;
        
        PathSegmentConnector destFrontConnector = roadMesh.PathSegmentConnectors[destFront];
        PathSegmentConnector destEndConnector = roadMesh.PathSegmentConnectors[destEnd];
        PathSegmentConnector frontConnector = roadMesh.PathSegmentConnectors[front];
        PathSegmentConnector endConnector = roadMesh.PathSegmentConnectors[end];
        
        int shortestConnector = end;
        int shortestDestConnector = destEnd;

        int shortestDistance = destEndConnector.DPath.Distances[shortestConnector];

        if (shortestDistance == -1)
            return null;

        int frontToEndDist = destEndConnector.DPath.Distances[front];
        if (frontToEndDist < shortestDistance)
        {
            shortestDistance = frontToEndDist;
            shortestConnector = front;
        }
        
        int endToFront = destFrontConnector.DPath.Distances[end];
        if (endToFront < shortestDistance)
        {
            shortestDistance = endToFront;
            shortestConnector = end;
            shortestDestConnector = destFront;
        }
        
        int frontToFront = destFrontConnector.DPath.Distances[front];
        if (frontToFront < shortestDistance)
        {
            shortestDistance = frontToFront;
            shortestConnector = front;
            shortestDestConnector = destFront;
        }

        DPath dpath = roadMesh.PathSegmentConnectors[shortestConnector].DPath;

        Stack<int> stackPath = new Stack<int>();

        int prevConnector = -1;
        
        // Console.WriteLine("Origin: " + shortestConnector);
        // Console.WriteLine("Destination" + shortestDestConnector);
        
        // Console.WriteLine("Origin ID: " + shortestDestConnector + " | Previous Path: " + String.Join(", ", dpath.Prevs));
        
        while (prevConnector != shortestDestConnector)
        {
            prevConnector = shortestDestConnector;
            stackPath.Push(shortestDestConnector);
            // Console.WriteLine("NODE ADDED TO PATH: " + shortestDestConnector);
            shortestDestConnector = dpath.Prevs[shortestDestConnector];
            // Console.WriteLine("Next Node: " + shortestDestConnector);
        }
        
        // Console.WriteLine("Traversal Path: " + String.Join(", ", stackPath.ToArray()));

        return stackPath;
    }

    public static void GenerateRandomCar()
    {
        int newCarEntity = EntityManager.AddEntity();

        //List<int> segments = ComponentManager.GetComponent<PathSegment>();
        (int randomSegment, int randomPathPoint) = GetRandomPathPoint();
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(randomSegment);
        Vector3 position = segment.Path[randomPathPoint];

        (int randomSegmentDestination, int randomPathPointDestination) = GetRandomPathPoint();

        Destination destination = new Destination()
        {
            SegmentID = randomSegmentDestination, //SegmentID = randomSegmentDestination,
            SegmentPathIndex = randomPathPointDestination + 1
        };

        Stack<int> shortestPath = GetShortestPath(segment, destination);
        destination.Path = shortestPath;

        if (destination.SegmentID == randomSegment)
        {
            destination.Path.Pop();
        }
        
        //there is a bug where if the cars/destination segment and points are both the same, then it will not 
        //generate a proper path and will crash
        //how do we want to handle this? retry or just remove path and destroy car?
        //for now we can just discard it I think
        if (randomPathPoint == randomPathPointDestination && randomSegment == randomSegmentDestination)
            return;

        Console.WriteLine("Destination Path Point: " + destination.SegmentPathIndex);
        
        List<Destination> destinations = new List<Destination>() {destination};

        Car car = new Car()
        {
            ConnectedSegment = segment.EntityID,
            GroupID = 0,
            Position = position,
            Color = Color.AliceBlue,
            Rotation = Matrix.Identity,
            Destinations = destinations,
            InitialPathIndex = randomPathPoint + 1
        };
        car.SegmentPath = BuildSegmentPath(car, destination);
        
        EntityManager.AddComponentToEntity<Car>(newCarEntity, car);
    }
}