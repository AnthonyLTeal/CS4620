using System;
using System.Collections.Generic;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlanetaryExpansion;

namespace CS4620IS;

public class CarSystems
{
    private const float UPDATE_TIME = 1000f;
    private static float _carTimer = 0;

    private static float iterations = 0;

    public static void BasicBehavior(GameTime gameTime)
    {
         List<int> entities = ComponentManager.GetComponent<Car>();

         _carTimer += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
         
         if (_carTimer < UPDATE_TIME)
             return;

         iterations += 1;
         
         Console.WriteLine(iterations);
         
         _carTimer = 0;
         
         foreach (int entity in entities)
         {
             Car car = ComponentManager.GetEntityComponent<Car>(entity);

             PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);

             if (car.Destinations.Count < 1)
             {
                 EntityManager.RemoveEntity(entity);
                 return;
             }

             int nextNode = car.Destinations[0].Path.Pop();
             
             if (car.Destinations[0].Path.Count == 0)
                 car.Destinations.RemoveAt(0);

             PathSegmentConnector nextConnector =
                 EntityManager.GetGlobalComponent<RoadMesh>().PathSegmentConnectors[nextNode];

             car.Position = nextConnector.Position;
             
             //int nextPathPoint = 
             //     DrawableAsset asset = ComponentManager.GetEntityComponent<DrawableAsset>(entity);
             //    
             //    move forward based on gps and current path
             //        recalculate path
             //    
             //    update position based current position and next node
         }
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
        
        Console.WriteLine("Origin: " + shortestConnector);
        Console.WriteLine("Destination" + shortestDestConnector);
        
        Console.WriteLine("Origin ID: " + shortestDestConnector + " | Previous Path: " + String.Join(", ", dpath.Prevs));
        
        while (prevConnector != shortestDestConnector)
        {
            prevConnector = shortestDestConnector;
            stackPath.Push(shortestDestConnector);
            Console.WriteLine("NODE ADDED TO PATH: " + shortestDestConnector);
            shortestDestConnector = dpath.Prevs[shortestDestConnector];
            Console.WriteLine("Next Node: " + shortestDestConnector);
        }
        
        Console.WriteLine("Traversal Path: " + String.Join(", ", stackPath.ToArray()));

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
            SegmentID = randomSegmentDestination,
            SegmentPathIndex = randomPathPointDestination
        };

        Stack<int> shortestPath = GetShortestPath(segment, destination);
        destination.Path = shortestPath;

        List<Destination> destinations = new List<Destination>() {destination};

        Car car = new Car()
        {
            ConnectedSegment = segment.EntityID,
            GroupID = 0,
            Position = position,
            Color = Color.AliceBlue,
            Rotation = Matrix.Identity,
            Destinations = destinations
        };
        
        EntityManager.AddComponentToEntity<Car>(newCarEntity, car);
    }
}