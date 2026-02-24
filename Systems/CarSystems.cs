using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Timers;
using CS4620IS.Collision;
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
         List<int> entitiesToRemove = new List<int>();
         RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();

         foreach (int entity in entities)
         {
             Car car = ComponentManager.GetEntityComponent<Car>(entity);
             PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);

             //rebuild path if it's changed?
             bool isPathValid = ValidPath(entity);
             if (!isPathValid)
             {
                 //Console.WriteLine("Pathing Update ");
                 car.Destinations[0].Path = GetShortestPath(connectedSegment, car.Destinations[0]);
                 //int nextConnector = car.Destinations[0].Path.Pop();
                 //car.CarPath = BuildSegmentPath(car, car.Destinations[0]); //shouldn't need this anymore, this is what we are trying to not have to use
             }

             if (car.Destinations.Count < 1)
             {
                 entitiesToRemove.Add(entity);
                 continue;
             } 
             
             float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
             MoveCar(entity, 2 * dt);

         }

         foreach (int entity in entitiesToRemove)
         {
             Console.WriteLine("Killing Entity: " + entity);
             EntityManager.RemoveEntity(entity);
         }
    }

    private static Vector3 GetIntersectionVertex(int direction, PathSegment segment)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegmentConnector connector;
        if (direction == -1)
        {
            connector = roadMesh.PathSegmentConnectors[(int)segment.FrontConnector];
        }
        else
        {
            connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
        }
        return connector.Position;
    }

    private static Vector3 GetPathVertexFromIndex(int index, PathSegment pathSegment)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        if (index <= -1)
        {
            Console.WriteLine();
            Console.WriteLine("Index -1 to front connector");
            Console.WriteLine();
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)pathSegment.FrontConnector];
            return connector.Position;
        } 
        if (index >= pathSegment.Path.Length)
        {
            Console.WriteLine();
            Console.WriteLine("Index +1 to end connector");
            Console.WriteLine();
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)pathSegment.EndConnector];
            return connector.Position;
        }
    
        return pathSegment.Path[index];
    }

    // private static Vector3 GetPathVertex(Car car)
    // {
    //     // PathSegment pathSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
    //     // Vector3 currentVertexPos = GetPathVertexFromIndex(car.CarPath.CurrentIndex, pathSegment);;
    //     // Vector3 nextVertexPos = GetPathVertexFromIndex(car.CarPath.CurrentIndex + car.CarPath.Direction, pathSegment);
    //     // //RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
    //     // Vector3 direction = Vector3.Normalize(nextVertexPos - currentVertexPos);
    //     // return nextVertexPos + RoadSideOffset(direction) * car.CurrentLane;
    //
    //     //return car.CarPath.Positions[car.CarPath.CurrentIndex];
    // }
    
    private static Vector3 BuildPathVertex(Car car, int i, int traverseDirection)
    {
        PathSegment pathSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        Vector3 currentVertexPos = pathSegment.Path[i];
        Vector3 nextVertexPos = pathSegment.Path[i + traverseDirection];//GetPathVertexFromIndex(i + traverseDirection, pathSegment);
        Vector3 direction = Vector3.Normalize(nextVertexPos - currentVertexPos);
        return nextVertexPos + RoadSideOffset(direction) * car.CurrentLane;
    }

    private static void BuildIntersectionPath(Car car, Vector3 p1, Vector3 p2, Vector3 connectorPosition)
    {
        //     Vector3 dir = Vector3.Slerp(v1, v2, 0.5f); //can use Slerp to get angles between which will make a smoother curve
        Vector3 p2Dir = Vector3.Normalize(p2 - connectorPosition);
        Vector3 p1Dir = Vector3.Normalize(connectorPosition - p1);
        Vector3 p2Offset = Vector3.Cross(p2Dir, Vector3.Up);
        Vector3 p1Offset = Vector3.Cross(p1Dir, Vector3.Up);
        
        float turnSign = MathF.Sign(Vector3.Cross(p1Dir, p2Dir).Y);
        
        Vector3 tangent = Vector3.Normalize(p1Dir - p2Dir);

        if (turnSign > 0)
            tangent = -tangent;
        
        //bool isLeftTurn = turnSign > 0;

        // PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        //
        // bool laneIsRightSide = (car.SegmentPath.Direction * (int)segment.PathDirection) == -1; // if that's how you define it
        //
        // bool isInside =
        //     (isLeftTurn && !laneIsRightSide) ||
        //     (!isLeftTurn && laneIsRightSide);
        //
        //
        // int modifier = 1;
        //
        // if (Vector3.Dot(p1Dir, p2Dir) < 0 && isInside)
        // {
        //     modifier = 3;
        // } 
        
        
        // float originalDistSq = Vector3.DistanceSquared(connectorPosition, p1);
        // float offsetDistSq   = Vector3.DistanceSquared(position, p1);
        //
        // bool isOutside = offsetDistSq > originalDistSq;
        
        Vector3 position = connectorPosition + tangent * car.CurrentLane;

        if (Vector3.Distance(position, p1) > Vector3.Distance(connectorPosition, p1))
        {
            //outside
        }
        
        car.OverridePath.Push(p2 - p2Offset * car.CurrentLane);
        car.OverridePath.Push(position);
        car.OverridePath.Push(p1 - p1Offset * car.CurrentLane);
    }

    // private static Vector3? forwardLookPoint(Car car)
    // {
    //     if (car.SegmentPath.CurrentIndex != car.SegmentPath.TopIndex &&
    //           car.SegmentPath.CurrentIndex != car.SegmentPath.BottomIndex)
    //         return null;
    //
    //     if (car.Destinations.Count == 0)
    //         return null;
    //
    //     if (car.Destinations[0].Path.Count == 0)
    // }

    private static bool CarToCarWillIntersect(Car sourceCar, Car targetCar, Vector3 sourceDir, float sourceVelocity)
    {
        Vector3 targetNext = targetCar.CarPath.Positions.Peek();
        Vector3 targetVelocity = targetNext - targetCar.Position;
        
        targetVelocity.Normalize();
        
        return SphereIntersection.WillIntersect(sourceCar.Position, targetCar.Position, sourceDir * sourceVelocity);
    }
    
    //TODO WaitOnTraffic isn't complete
    //For cars that are on a connector, before they've left a connector, they should check that the road ahead isn't backed up
    //not just wait on the intersection
    private static bool WaitOnTraffic(int entity, float velocity, Vector3 direction)
    {
        Car car = ComponentManager.GetEntityComponent<Car>(entity);
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();

        //Vector3 predPosition = car.Position + velocity * direction;

        if (car.OnConnector != -1)
        {
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[car.OnConnector];
            if (connector.SegmentEntities.Count > 2) //no need to wait on traffic if we are waiting on stop queue
                return false;
        }
        
        // if (car.OnConnector == -1)
        //TODO fix this for inside turns on road connectors (not intersections)
        {
            foreach (int targetCarEntity in segment.EntitiesOnSegment) //wait on path on segment only?
            {
                Car targetCar = ComponentManager.GetEntityComponent<Car>(targetCarEntity);
                
                if (car.OnConnector != -1 && targetCar.OnConnector != -1)
                {
                    Vector3 targetCarDir = Vector3.Normalize(targetCar.OverridePath.Peek() - targetCar.Position);
                    if (Vector3.Dot(targetCarDir, direction) < 0)
                        continue;
                }
                
                if (targetCar.CarPath.Direction != car.CarPath.Direction)
                    continue;
                if (targetCarEntity == entity)
                    continue;
                if (CarToCarWillIntersect(car, targetCar, direction, velocity))
                    return true;
            }
        }
        
        //I think we just check the next path destination to see if we are hitting a connector
        if (car.Destinations[0].Path.Count > 0)
        {
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[car.Destinations[0].Path.Peek()];

            if (connector.SegmentEntities.Count > 2) //check against entering intersection
            {
                foreach (int targetCarEntity in connector.StopQueue)
                {
                    Car targetCar = ComponentManager.GetEntityComponent<Car>(targetCarEntity);
                    // if (targetCar.SegmentPath.Direction != car.SegmentPath.Direction)
                    //     continue;
                    if (targetCarEntity == entity)
                        continue;
                    if (CarToCarWillIntersect(car, targetCar, direction, velocity))
                        return true;
                }
            }
            
            else //check against entering connector that has only 2 segments so isn't an intersection
            {
                foreach (int nextSegmentEntity in connector.SegmentEntities)
                {
                    if (nextSegmentEntity == car.ConnectedSegment)
                        continue;
            
                    PathSegment nextSegment = ComponentManager.GetEntityComponent<PathSegment>(nextSegmentEntity);
                
                    foreach (int targetCarEntity in nextSegment.EntitiesOnSegment)
                    {
                        Car targetCar = ComponentManager.GetEntityComponent<Car>(targetCarEntity);
                        // if (targetCar.SegmentPath.Direction != car.SegmentPath.Direction)
                        //     continue;
                        if (targetCarEntity == entity)
                            continue;
            
                        if (CarToCarWillIntersect(car, targetCar, direction, velocity))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    private static bool ValidPath(int carEntity)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        Car car = ComponentManager.GetEntityComponent<Car>(carEntity);

        if (car.Destinations.Count == 0)
            return true;
        
        if (car.Destinations[0].Path.Count == 0)
            return true;
        
        int nextConnectorId = car.Destinations[0].Path.Peek();
        PathSegmentConnector nextConnector = roadMesh.PathSegmentConnectors[nextConnectorId];
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);

        if (segment.Path.Length != car.CarPath.PathSize)
            return false;
        
        // if (car.Destinations[0].Path.Count > 1)
        // {
        //     if (segment.EndConnector != nextConnectorId && segment.FrontConnector != nextConnectorId)
        //         return false;
        // }
        
        return true;
    }
    
    private static bool WaitOnStopSign(int carEntity)
    {
        Car car = ComponentManager.GetEntityComponent<Car>(carEntity);
        
        if (car.OnConnector == -1)
            return false;
        
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegmentConnector connector = roadMesh.PathSegmentConnectors[car.OnConnector];

        if (connector.SegmentEntities.Count <= 2)
            return false;

        if (carEntity == connector.StopQueue.Peek())
            return false;

        return true;
    }

    //TODO need to add collision check here for cars so they don't hit/pass through each other, especially at intersections
    private static void MoveCar(int entity, float velocity)
    {
        Car car = ComponentManager.GetEntityComponent<Car>(entity);
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();

        float remaining = velocity;
        while (remaining > 0.000001f)
        {
            if (car.CarPath.Positions.Count == 0)
            {
                car.Destinations.RemoveAt(0);
                return;
            }
            
            Vector3 nextPoint = car.CarPath.Positions.Peek();
            // if (isOverridden)
            //     nextPoint = car.OverridePath.Peek();
            
            Vector3 toTarget = nextPoint - car.Position;
            float distanceTo = toTarget.Length();
            
            Vector3 direction = distanceTo > 0f ? toTarget / distanceTo : Vector3.Zero;
            
            if (distanceTo > remaining)
            {
                // if (WaitOnTraffic(entity, remaining, direction))
                //     return;
                
                car.Position += direction * remaining;
                car.Rotation = Matrix.CreateWorld(Vector3.Zero, direction, Vector3.Up);
                return;
            }
            
            remaining -= distanceTo;
            car.Position = car.CarPath.Positions.Dequeue();
            

            
            // bool isOverridden = (car.OverridePath.Count > 0);
            // PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
            //
            // if (WaitOnStopSign(entity))
            //     break;
            //
            // Vector3 nextPoint = car.CarPath.Positions[car.CarPath.CurrentIndex];
            //
            // if (isOverridden)
            //     nextPoint = car.OverridePath.Peek();
            //
            // Vector3 toTarget = nextPoint - car.Position;
            // float distanceTo = toTarget.Length();
            //
            // Vector3 direction = distanceTo > 0f ? toTarget / distanceTo : Vector3.Zero;
            // // float remaining = velocity - distanceTo;
            //
            // if (distanceTo > remaining)
            // {
            //     if (WaitOnTraffic(entity, remaining, direction))
            //         return;
            //     
            //     car.Position += direction * remaining;
            //     car.Rotation = Matrix.CreateWorld(Vector3.Zero, direction, Vector3.Up);
            //     return;
            // }
            //
            // remaining -= distanceTo;
            //
            // if (WaitOnTraffic(entity, remaining, direction))
            //     return;
            //
            // if (isOverridden) //if overidden, car is traveling through an intersection/connector, but not necessarily an intersection
            // {
            //     car.Position = car.OverridePath.Pop();
            //     if (car.OverridePath.Count > 0)
            //         continue;
            //     car.Position = car.CarPath.Positions[car.CarPath.CurrentIndex];
            //     car.CarPath.CurrentIndex += 1; //we will want to skip the first segment point
            //     if (car.OverridePath.Count == 0 && car.OnConnector != -1)
            //     {
            //         PathSegmentConnector connectorQueued = roadMesh.PathSegmentConnectors[car.OnConnector];
            //         if (connectorQueued.SegmentEntities.Count() > 2)
            //         {
            //             connectorQueued.StopQueue.Dequeue();
            //         }
            //         car.OnConnector = -1;
            //     }
            // }
            // else
            // {
            //     car.CarPath.CurrentIndex += 1;
            //     car.Position = nextPoint;
            // }
            //
            // bool finalSegment = (car.Destinations[0].Path.Count == 0);
            // if ((car.CarPath.CurrentIndex == car.CarPath.Positions.Count - 1 && 
            //      finalSegment))
            // {
            //     car.Destinations.RemoveAt(0);
            //     connectedSegment.EntitiesOnSegment.Remove(entity);
            //     return;
            // }
            //
            // if (car.CarPath.CurrentIndex == car.CarPath.Positions.Count - 1 &&
            //     !finalSegment)
            // {
            //     int lastConnectorID = car.Destinations[0].Path.Pop();
            //
            //     Vector3 p1 = car.CarPath.Positions.Dequeue();
            //     
            //     car.ConnectedSegment = GetNextSegment(car, lastConnectorID);
            //     car.CarPath = BuildSegmentPath(car, car.Destinations[0], lastConnectorID);
            //     
            //     PathSegment newSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
            //     connectedSegment.EntitiesOnSegment.Remove(entity);
            //     newSegment.EntitiesOnSegment.Add(entity);
            //     Vector3 p2 = car.CarPath.Positions[car.CarPath.CurrentIndex + 1];
            //     Vector3 p3 = roadMesh.PathSegmentConnectors[lastConnectorID].Position;
            //     BuildIntersectionPath(car, p1, p2, p3);
            //     car.Position = car.OverridePath.Pop();
            //     
            //     car.OnConnector = lastConnectorID;
            //     PathSegmentConnector lastConnector = roadMesh.PathSegmentConnectors[lastConnectorID];
            //     
            //     if (lastConnector.SegmentEntities.Count > 2)
            //         lastConnector.StopQueue.Enqueue(entity);
            // }
        }
    }

    private static Vector3 RoadSideOffset(Vector3 direction)
    {
        //Console.WriteLine("DIRECTION: " + direction);
        //Console.WriteLine("CALCULATED OFFSET: " + Vector3.Cross(Vector3.Up, direction));
        return Vector3.Cross(Vector3.Up, direction);
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

        //TODO we are sometimes returning 0 from here still, need to look into this 
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

    private static CarPath BuildSegmentPath(Car car,  Destination destination, int? lastConnectorID = null)
    {
        PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        Console.WriteLine("Segment Path Size: " + connectedSegment.TotalPathLength);
        
        int finalIndex = connectedSegment.Path.Length - 1; //need to treat this like an index for an array
        int traverseDir = 1;
        int index = 0;
        
        if (lastConnectorID != null) //from connector to connecter
        {
            if (connectedSegment.EndConnector == lastConnectorID)
            {
                traverseDir = -1;
                index = connectedSegment.Path.Length - 1; //need to treat this like an index for an array
                finalIndex = 0;
            }
        }
        
        if (lastConnectorID == null)  //from segment
        {
            index = car.InitialPathIndex;
            if (destination.Path.Count > 0) //from segment to connector
            {
                int nextDest = car.Destinations[0].Path.Peek();
                if (nextDest == connectedSegment.FrontConnector)
                {
                    traverseDir = -1;
                    finalIndex = 0;
                }
            }
            else //from segment to segment
            {
                if (destination.SegmentPathIndex > index)
                    traverseDir = 1;
                else
                    traverseDir = -1;
            }
        }
        
        if (destination.Path.Count == 0) //from point to segment
        {
            finalIndex = destination.SegmentPathIndex;
        }

        CarPath carPath = new CarPath();
        carPath.Direction = traverseDir;
        carPath.PathSize = connectedSegment.Path.Length;
        
        Console.WriteLine("Initial Index: " + index);
        Console.WriteLine("Direction: " + traverseDir);
        Console.WriteLine("End: " + finalIndex);
        
        Queue<Vector3> positions = new Queue<Vector3>();
        while (index != finalIndex)
        {
            Console.WriteLine("Index: " + index);
            positions.Enqueue(BuildPathVertex(car, index, carPath.Direction));
            index += carPath.Direction;
        }

        carPath.CurrentIndex = 0;
        carPath.Positions = positions;
        
        return carPath;
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
        int randomPathPoint = random.Next(1, segment.Path.Length - 1); //don't let it be the last or the first point on a path, this is the start of an intersection
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
        
        //Console.WriteLine("Traversal Path: " + String.Join(", ", stackPath.ToArray()));

        return stackPath;
    }

    public static Vector3 GetInitialPosition(Car car)
    {
        PathSegment pathSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        //Vector3 currentVertexPos = GetPathVertexFromIndex(car.CarPath.CurrentIndex, pathSegment);;
        //Vector3 nextVertexPos = GetPathVertexFromIndex(car.InitialPathIndex + car.CarPath.Direction, pathSegment);
        Vector3 currentVertexPos = car.CarPath.Positions.Dequeue();
        Vector3 nextVertexPos = pathSegment.Path[car.InitialPathIndex + car.CarPath.Direction];
        //RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        Vector3 direction = Vector3.Normalize(nextVertexPos - currentVertexPos);
        return currentVertexPos + RoadSideOffset(direction) * car.CurrentLane;
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
            SegmentPathIndex = randomPathPointDestination
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
        {
            EntityManager.RemoveEntity(newCarEntity);
            return;
        }

        Console.WriteLine("Initial Segment: " + randomSegment);
        Console.WriteLine("Initial Path Point: " + randomPathPoint);
        Console.WriteLine("Destination Segment: " + destination.SegmentID);
        Console.WriteLine("Destination Path Point: " + destination.SegmentPathIndex);
        
        List<Destination> destinations = new List<Destination>() {destination};
        
        Console.WriteLine(segment.EntityID);

        Car car = new Car()
        {
            ConnectedSegment = segment.EntityID,
            GroupID = 0,
            Position = position,
            Color = Color.LightGreen,
            Rotation = Matrix.Identity,
            Destinations = destinations,
            InitialPathIndex = randomPathPoint
        };
        car.CarPath = BuildSegmentPath(car, destination);
        
        Console.WriteLine("Car Path Length: " + car.CarPath.Positions.Count);
        car.Position = GetInitialPosition(car);
        
        EntityManager.AddComponentToEntity(newCarEntity, car);

        //might be better to add these to a list to be added when there is free space on the road rather than just not spawning them
        //let's see if this even works though lol
        // if (WaitOnTraffic(newCarEntity, 0, Vector3.Zero))
        // {
        //     EntityManager.RemoveEntity(newCarEntity);
        //     return;
        // }
        
        segment.EntitiesOnSegment.Add(newCarEntity);
    }
}