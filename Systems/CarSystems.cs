using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using CS4620IS.Collision;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using PlanetaryExpansion;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace CS4620IS;

public class CarSystems
{
    public static void BasicBehavior(GameTime gameTime)
    {
         List<int> entities = ComponentManager.GetComponent<Car>();
         List<int> entitiesToRemove = new List<int>();

         foreach (int entity in entities)
         {
             Car car = ComponentManager.GetEntityComponent<Car>(entity);
             
             PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);

             //destroy car if segment entity is dead
             if (connectedSegment is null)
             {
                 Console.WriteLine("Null Segment");
                 car.Destinations.Clear();
             }

             //destroy if destination entity is dead
             if (car.Destinations.Count > 0)
             {
                 PathSegment destinationSegment = ComponentManager.GetEntityComponent<PathSegment>((car.Destinations[0].TargetSegmentID));

                 if (destinationSegment is null)
                 {
                     Console.WriteLine("Null Destination");
                     car.Destinations.Clear();
                 }
             }
             
             if (car.Destinations.Count > 0 && !PathExists(car.Destinations[0], car.ConnectedSegment))
             {
                 Console.WriteLine("No valid path");
                 car.Destinations.Clear();
             }
             
             if (car.Destinations.Count < 1)
             {
                 entitiesToRemove.Add(entity);
                 foreach (string comment in car.Log)
                 {
                     Console.WriteLine(comment);
                 }
                 Console.WriteLine();
                 continue;
             }

             ValidateDestinationSegment(car.Destinations[0], car);
             ValidateCurrentSegment(car, entity);
             
             float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
             SimulationSuper simulationSuper = EntityManager.GetGlobalComponent<SimulationSuper>();
             MoveCar(entity, 2 * dt * simulationSuper.SimSpeed);

         }

         foreach (int entity in entitiesToRemove)
         {
             //Console.WriteLine("Killing Entity: " + entity);
             EntityManager.RemoveEntity(entity);
         }
    }

    private static bool PathExists(Destination destination, int segmentEntity)
    {
        PathSegment segmentOne = ComponentManager.GetEntityComponent<PathSegment>(destination.TargetSegmentID);
        PathSegment segmentTwo = ComponentManager.GetEntityComponent<PathSegment>(segmentEntity);

        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();

        PathSegmentConnector destinationConnector = roadMesh.PathSegmentConnectors[(int)segmentOne.EndConnector];
        return !(destinationConnector.DPath.Prevs[(int)segmentTwo.EndConnector] == -1);
    }

    private static Vector3 GetPathVertexFromIndex(int index, PathSegment pathSegment)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        if (index == -1)
        {
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)pathSegment.FrontConnector];
            return connector.Position;
        } 
        if (index == pathSegment.Path.Length)
        {
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)pathSegment.EndConnector];
            return connector.Position;
        }

        return pathSegment.Path[index];
    }

    private static Vector3 GetPathVertex(Car car)
    {
        PathSegment pathSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        Vector3 currentVertexPos = GetPathVertexFromIndex(car.SegmentPath.CurrentIndex, pathSegment);;
        Vector3 nextVertexPos = GetPathVertexFromIndex(car.SegmentPath.CurrentIndex + car.SegmentPath.Direction, pathSegment);
        //RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        Vector3 direction = Vector3.Normalize(nextVertexPos - currentVertexPos);
        return nextVertexPos + RoadSideOffset(direction) * car.CurrentLane;
    }

    private static void BuildIntersectionPath(Car car, Vector3 p1, Vector3 p2, Vector3 connectorPosition)
    {
        //     Vector3 dir = Vector3.Slerp(v1, v2, 0.5f); //can use Slerp to get angles between.
        Vector3 p2Dir = Vector3.Normalize(p2 - connectorPosition);
        Vector3 p1Dir = Vector3.Normalize(connectorPosition - p1);
        Vector3 p2Offset = Vector3.Cross(p2Dir, Vector3.Up);
        Vector3 p1Offset = Vector3.Cross(p1Dir, Vector3.Up);
        
        float turnSign = MathF.Sign(Vector3.Cross(p1Dir, p2Dir).Y);
        
        Vector3 tangent = Vector3.Normalize(p1Dir - p2Dir);

        if (turnSign > 0)
            tangent = -tangent;
        
        bool isLeftTurn = turnSign > 0;

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

    private static int GetNextConnectorID(Destination destination, int lastConnectorId)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegment targetSegment = ComponentManager.GetEntityComponent<PathSegment>(destination.TargetSegmentID);

        if (targetSegment.FrontConnector == lastConnectorId || targetSegment.EndConnector == lastConnectorId)
            return -1;
        
        PathSegmentConnector endConnector = roadMesh.PathSegmentConnectors[(int)targetSegment.EndConnector];
        PathSegmentConnector frontConnector = roadMesh.PathSegmentConnectors[(int)targetSegment.FrontConnector];
        if (endConnector.DPath.Distances[lastConnectorId] < frontConnector.DPath.Distances[lastConnectorId])
            return endConnector.DPath.Prevs[lastConnectorId];
        return frontConnector.DPath.Prevs[lastConnectorId];
    }

    /// <summary>
    /// Used to change the cars connector ID when the car is on a path that was sliced
    /// and their index count puts them on the new segment, meaning their previous
    /// ID is invalid
    /// </summary>
    // public static int ValidateLastConnector(Car car, int pathSegmentID)
    // {
    //     return 0;
    // }
    //
    // public static int ValidateNextConnector(Car car, int pathSegmentID, int lastConnector)
    // {
    //     return lastConnector;
    // }

    public static void ValidateDestinationSegment(Destination destination, Car car)
    {
        //todo look into this maybe in the future for a weird edge case
        //the car direction can matter here, if we slice at the point of the destination, and the car is on the
        //point of intersection as well (just before or after), then it will set it to the first index of the new
        //segment always. We don't always want this, if it's negative direction, we'd want to set it to the 
        //old segment at the last point, but this might not be needed when cars go to buildings directly
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(destination.TargetSegmentID);
        if (destination.TargetPathIndex >= segment.Path.Length)
        {
            car.Log.Add("Previous Destination Segment ID: " + destination.TargetSegmentID);
            car.Log.Add("Previous Destination Path Point: " + destination.TargetPathIndex);
            RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
            
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
            destination.TargetSegmentID = connector.SegmentEntities[1];
            destination.TargetPathIndex = Math.Max(destination.TargetPathIndex - segment.Path.Length - 1, 0);
            
            car.Log.Add("New Destination Segment ID: " + destination.TargetSegmentID);
            car.Log.Add("New Destination Path Point: " + destination.TargetPathIndex);
        }
    }

    public static void ValidateCurrentSegment(Car car, int entity)
    {
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        
        if (car.SegmentPath.SegmentSize != segment.Path.Count())
        {
            car.Log.Add("-----------------------------------");
            car.Log.Add("Target Destination Segment: " + car.Destinations[0].TargetSegmentID);
            car.Log.Add("Target Destination Index: " + car.Destinations[0].TargetPathIndex);
            car.Log.Add("Previous Connected Segment:" + car.connectedSegment);
            car.Log.Add("Previous Connected Segment Size: " + car.SegmentPath.SegmentSize);
            car.Log.Add("Previous Direction: " + car.SegmentPath.Direction);
            car.Log.Add("Previous Index:" + car.SegmentPath.CurrentIndex);

            if (car.SegmentPath.Direction == -1)
            {
                if (car.SegmentPath.CurrentIndex > segment.Path.Length + 1)
                {
                    car.Log.Add("Branch: 1");

                    car.InitialPathIndex = Math.Max(car.SegmentPath.CurrentIndex - segment.Path.Length - 2, 0);
                    RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
                    PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
                    car.connectedSegment = connector.SegmentEntities[1];
                }

                //todo this should eventually create a intersection override as well
                else if (car.SegmentPath.CurrentIndex >= segment.Path.Length)
                {
                    car.Log.Add("Branch: 2");
                    car.InitialPathIndex = segment.Path.Length - 1;
                    RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
                    PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
                    car.connectedSegment = connector.SegmentEntities[0];
                }

                else
                {
                    car.Log.Add("Branch: 3");
                    car.SegmentPath.SegmentSize = segment.Path.Length;
                    car.InitialPathIndex = car.SegmentPath.CurrentIndex;
                }
            }
            else
            {
                if (car.SegmentPath.CurrentIndex > segment.Path.Length)
                {
                    car.Log.Add("Branch: 4");
                    car.InitialPathIndex = Math.Max(car.SegmentPath.CurrentIndex - segment.Path.Length - 2, 0);
                    RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
                    PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
                    car.connectedSegment = connector.SegmentEntities[1];
                }
                
                //todo this should eventually create a intersection override as well
                else if (car.SegmentPath.CurrentIndex >= segment.Path.Length - 1)
                {
                    
                    car.Log.Add("Branch: 5");
                    car.InitialPathIndex = 0;
                    RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
                    PathSegmentConnector connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
                    car.connectedSegment = connector.SegmentEntities[1];
                }

                else
                {
                    car.Log.Add("Branch: 6");
                    car.SegmentPath.SegmentSize = segment.Path.Length;
                    car.InitialPathIndex = car.SegmentPath.CurrentIndex;
                }
            }
            
            car.Log.Add("New Car Initial Index: " + car.InitialPathIndex);
            car.Log.Add("New Car Connected Segment:" + car.connectedSegment);

            car.SegmentPath = BuildCarPath(car, car.Destinations[0]);
            car.Log.Add("Direction: " + car.SegmentPath.Direction);
            car.Log.Add("New Connected Segment Size: " + car.SegmentPath.SegmentSize);

            Console.WriteLine();
        }
    }

    private static bool CarToCarWillIntersect(Car sourceCar, Car targetCar, Vector3 sourceDir, float sourceVelocity)
    {
        Vector3 targetNext = GetPathVertex(targetCar);
        Vector3 targetVelocity = targetNext - targetCar.Position;
        
        targetVelocity.Normalize();
        
        return SphereIntersection.WillIntersect(sourceCar.Position, targetCar.Position, sourceDir * sourceVelocity);
    }
    
    //TODO WaitOnTraffic isn't complete
    //For cars that are on a connector, before they've left a connector, they should check that the road ahead isn't backed up
    //not just wait on the intersection
    // private static bool WaitOnTraffic(int entity, float velocity, Vector3 direction)
    // {
    //     Car car = ComponentManager.GetEntityComponent<Car>(entity);
    //     PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
    //     RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
    //
    //     //Vector3 predPosition = car.Position + velocity * direction;
    //
    //     if (car.OnConnector != -1)
    //     {
    //         PathSegmentConnector connector = roadMesh.PathSegmentConnectors[car.OnConnector];
    //         if (connector.SegmentEntities.Count > 2) //no need to wait on traffic if we are waiting on stop queue
    //             return false;
    //     }
    //     
    //     // if (car.OnConnector == -1)
    //     //TODO fix this for inside turns on road connectors (not intersections)
    //     {
    //         foreach (int targetCarEntity in segment.EntitiesOnSegment) //wait on path on segment only?
    //         {
    //             Car targetCar = ComponentManager.GetEntityComponent<Car>(targetCarEntity);
    //             
    //             if (car.OnConnector != -1 && targetCar.OnConnector != -1)
    //             {
    //                 Vector3 targetCarDir = Vector3.Normalize(targetCar.OverridePath.Peek() - targetCar.Position);
    //                 if (Vector3.Dot(targetCarDir, direction) < 0)
    //                     continue;
    //             }
    //             
    //             if (targetCar.SegmentPath.Direction != car.SegmentPath.Direction)
    //                 continue;
    //             if (targetCarEntity == entity)
    //                 continue;
    //             if (CarToCarWillIntersect(car, targetCar, direction, velocity))
    //                 return true;
    //         }
    //     }
    //     
    //     //I think we just check the next path destination to see if we are hitting a connector
    //     if (car.Destinations[0].Path.Count > 0)
    //     {
    //         PathSegmentConnector connector = roadMesh.PathSegmentConnectors[car.Destinations[0].Path.Peek()];
    //
    //         if (connector.SegmentEntities.Count > 2) //check against entering intersection
    //         {
    //             foreach (int targetCarEntity in connector.StopQueue)
    //             {
    //                 Car targetCar = ComponentManager.GetEntityComponent<Car>(targetCarEntity);
    //                 // if (targetCar.SegmentPath.Direction != car.SegmentPath.Direction)
    //                 //     continue;
    //                 if (targetCarEntity == entity)
    //                     continue;
    //                 if (CarToCarWillIntersect(car, targetCar, direction, velocity))
    //                     return true;
    //             }
    //         }
    //         
    //         else //check against entering connector that has only 2 segments so isn't an intersection
    //         {
    //             foreach (int nextSegmentEntity in connector.SegmentEntities)
    //             {
    //                 if (nextSegmentEntity == car.ConnectedSegment)
    //                     continue;
    //         
    //                 PathSegment nextSegment = ComponentManager.GetEntityComponent<PathSegment>(nextSegmentEntity);
    //             
    //                 foreach (int targetCarEntity in nextSegment.EntitiesOnSegment)
    //                 {
    //                     Car targetCar = ComponentManager.GetEntityComponent<Car>(targetCarEntity);
    //                     // if (targetCar.SegmentPath.Direction != car.SegmentPath.Direction)
    //                     //     continue;
    //                     if (targetCarEntity == entity)
    //                         continue;
    //         
    //                     if (CarToCarWillIntersect(car, targetCar, direction, velocity))
    //                     {
    //                         return true;
    //                     }
    //                 }
    //             }
    //         }
    //     }
    //
    //     return false;
    // }
    
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
            bool isOverridden = (car.OverridePath.Count > 0);
            PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
            
            if (WaitOnStopSign(entity))
                break;
            
            Vector3 nextPoint = GetPathVertex(car);
            if (isOverridden)
                nextPoint = car.OverridePath.Peek();
            //Console.WriteLine("Car Direction " + car.SegmentPath.Direction);
            //Console.WriteLine("Car Current Index " + car.SegmentPath.CurrentIndex);
            
            Vector3 toTarget = nextPoint - car.Position;
            float distanceTo = toTarget.Length();

            Vector3 direction = distanceTo > 0f ? toTarget / distanceTo : Vector3.Zero;
            
            if (distanceTo > remaining)
            {
                // if (WaitOnTraffic(entity, remaining, direction))
                    // return;
                
                car.Position += direction * remaining;
                car.Rotation = Matrix.CreateWorld(Vector3.Zero, direction, Vector3.Up);
                return;
            }

            remaining -= distanceTo;
            
            bool finalSegment = (car.connectedSegment == car.Destinations[0].TargetSegmentID);
            if (car.SegmentPath.CurrentIndex == car.Destinations[0].TargetPathIndex && finalSegment)
            {
                car.Destinations.RemoveAt(0);
                connectedSegment.EntitiesOnSegment.Remove(entity);
                return;
            }
            
            // Console.WriteLine("Target Segment: " + car.Destinations[0].TargetSegmentID);
            // Console.WriteLine("Target Point Index: " + car.Destinations[0].TargetPathIndex);
            //
            // Console.WriteLine("Current Segment: " + car.connectedSegment);
            // Console.WriteLine("Current Point Index: " + car.SegmentPath.CurrentIndex);
            // if (WaitOnTraffic(entity, remaining, direction))
            //     return;

            if (isOverridden) //if overidden, car is traveling through an intersection/connector, but not necessarily an intersection
            {
                car.Position = car.OverridePath.Pop();
                if (car.OverridePath.Count > 0)
                    continue;
                //car.Position = GetPathVertex(car);
                //car.SegmentPath.CurrentIndex += car.SegmentPath.Direction; //we will want to skip the first segment point
                if (car.OverridePath.Count == 0 && car.OnConnector != -1)
                {
                    PathSegmentConnector connectorQueued = roadMesh.PathSegmentConnectors[car.OnConnector];
                    if (connectorQueued.SegmentEntities.Count() > 2)
                    {
                        connectorQueued.StopQueue.Dequeue();
                    }
                    car.OnConnector = -1;
                }
            }
            else
            {
                car.SegmentPath.CurrentIndex += car.SegmentPath.Direction;
                car.Position = nextPoint;
            }
            
            if (((car.SegmentPath.CurrentIndex == 0 && car.SegmentPath.Direction == -1) || 
                (car.SegmentPath.CurrentIndex == car.SegmentPath.SegmentSize - 1  && car.SegmentPath.Direction == 1)))
            {
                int lastConnectorID = car.SegmentPath.NextConnectorID;
                int nextConnectorId = GetNextConnectorID(car.Destinations[0], lastConnectorID);
                int lastPathIndex = car.SegmentPath.CurrentIndex;
                PathSegment previousSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
                previousSegment.EntitiesOnSegment.Remove(entity);
                int nextSegmentId = GetNextSegment(car.Destinations[0], nextConnectorId, lastConnectorID);
                PathSegment nextSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
                nextSegment.EntitiesOnSegment.Add(entity);
                car.connectedSegment = nextSegmentId;

                Vector3 p1 = GetPathVertexFromIndex(car.SegmentPath.CurrentIndex, connectedSegment);

                car.SegmentPath = BuildCarPath(car, car.Destinations[0], lastConnectorID, nextConnectorId);
                //car.SegmentPath.CurrentIndex += car.SegmentPath.Direction;
                
                //might need to change this
                //car.Position = nextPoint;
                car.OnConnector = lastConnectorID;
                PathSegment newSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
                connectedSegment.EntitiesOnSegment.Remove(entity);
                newSegment.EntitiesOnSegment.Add(entity);
                Vector3 p2 = GetPathVertexFromIndex(car.SegmentPath.CurrentIndex, newSegment);
                Console.WriteLine("Current Path Index: " + car.SegmentPath.CurrentIndex);
                Console.WriteLine("Current Path Size: " + car.SegmentPath.SegmentSize);
                Vector3 p3 = roadMesh.PathSegmentConnectors[lastConnectorID].Position;
                BuildIntersectionPath(car, p1, p2, p3);
                car.Position = car.OverridePath.Pop();
                Console.WriteLine("Override Length: " + car.OverridePath.Count);
                car.OnConnector = lastConnectorID;
                PathSegmentConnector lastConnector = roadMesh.PathSegmentConnectors[lastConnectorID];
                if (lastConnector.SegmentEntities.Count > 2) 
                    lastConnector.StopQueue.Enqueue(entity);
            }
        }
    }

    private static Vector3 RoadSideOffset(Vector3 direction)
    {
        //Console.WriteLine("DIRECTION: " + direction);
        //Console.WriteLine("CALCULATED OFFSET: " + Vector3.Cross(Vector3.Up, direction));
        return Vector3.Cross(Vector3.Up, direction);
    }

    private static int GetNextSegment(Destination destination, int nextConnectorID, int lastConnectorID)
    {
        PathSegmentConnector connector = EntityManager.GetGlobalComponent<RoadMesh>().PathSegmentConnectors[lastConnectorID];
        foreach (int segmentEntity in connector.SegmentEntities)
        {
            if (segmentEntity == destination.TargetSegmentID)
                return segmentEntity;
            
            PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(segmentEntity);
            if ((segment.EndConnector == nextConnectorID && segment.FrontConnector == lastConnectorID) ||
                (segment.EndConnector == lastConnectorID && segment.FrontConnector == nextConnectorID))
            {
                return segmentEntity;
            }
        }

        return -1; 
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

    private static CarPath BuildCarPath(Car car,  Destination destination, int lastConnectorID = -1, int nextConnectorID = -1)
    {
        CarPath carPath = new CarPath();
        PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        carPath.SegmentSize = connectedSegment.Path.Length;
        
        //Console.WriteLine("Next Connector: " + nextConnectorID);
        
        int nextConnector;
        if (lastConnectorID == -1 && car.Destinations[0].TargetSegmentID != car.connectedSegment) //new car target on different segment
        {
            nextConnector = GetInitialConnector(connectedSegment, destination);
            //Console.WriteLine("Next Connector From Initial: " + nextConnector);
            
            carPath.CurrentIndex = car.InitialPathIndex;
            if (nextConnector == connectedSegment.FrontConnector)
                carPath.Direction = -1;
            else
                carPath.Direction = 1;
            carPath.NextConnectorID = nextConnector;
            carPath.LastConnectorID = -1;
            return carPath;
        }

        if (lastConnectorID == -1 && car.Destinations[0].TargetSegmentID == car.connectedSegment) //new car target on same segment
        {
            carPath.CurrentIndex = car.InitialPathIndex;
            if (car.InitialPathIndex > car.Destinations[0].TargetPathIndex)
                carPath.Direction = -1;
            else
                carPath.Direction = 1;
            carPath.NextConnectorID = -1;
            carPath.LastConnectorID = -1;
            return carPath;
        }

        if (car.Destinations[0].TargetSegmentID == car.connectedSegment) //from connector to final segment
        {
            //Console.WriteLine("Final Segment");
            if (lastConnectorID == connectedSegment.EndConnector)
            {
                carPath.CurrentIndex = connectedSegment.Path.Length - 1;
                carPath.Direction = -1;
            }
            else
            {
                carPath.CurrentIndex = 0;
                carPath.Direction = 1;
            }

            carPath.NextConnectorID = -1;
            carPath.LastConnectorID = lastConnectorID;
            return carPath;
        }
        
        //from connector to connector
        if (lastConnectorID == connectedSegment.EndConnector)
        {
            carPath.CurrentIndex = connectedSegment.Path.Length - 1;
            carPath.Direction = -1;
        }
        else
        {
            carPath.CurrentIndex = 0;
            carPath.Direction = 1;
        }

        carPath.NextConnectorID = nextConnectorID;
        carPath.LastConnectorID = lastConnectorID;
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
        if (segment.Path.Length < 3)
            return (-1, -1);
        int randomPathPoint = random.Next(1, segment.Path.Length - 2); //don't let it be the last or the first point on a path, this is the start of an intersection
        return (segment.EntityID, randomPathPoint);
    }

    public static int GetInitialConnector(PathSegment currentSegment, Destination destination)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegment destSegment = ComponentManager.GetEntityComponent<PathSegment>(destination.TargetSegmentID);

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
            return -1;

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

        return shortestConnector;
    }

    public static Vector3 GetInitialPosition(Car car)
    {
        PathSegment pathSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        Vector3 currentVertexPos = GetPathVertexFromIndex(car.SegmentPath.CurrentIndex, pathSegment);;
        Vector3 nextVertexPos = GetPathVertexFromIndex(car.SegmentPath.CurrentIndex + car.SegmentPath.Direction, pathSegment);
        //RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        Vector3 direction = Vector3.Normalize(nextVertexPos - currentVertexPos);
        return currentVertexPos + RoadSideOffset(direction) * car.CurrentLane;
    }

    public static void GenerateRandomCar()
    {

        //List<int> segments = ComponentManager.GetComponent<PathSegment>();
        (int randomSegment, int randomPathPoint) = GetRandomPathPoint();
        if (randomSegment == -1)
            return;
        
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(randomSegment);
        Vector3 position = segment.Path[randomPathPoint];

        (int randomSegmentDestination, int randomPathPointDestination) = GetRandomPathPoint();
        if (randomSegmentDestination == -1)
            return;

        Destination destination = new Destination()
        {
            TargetSegmentID = randomSegmentDestination, //SegmentID = randomSegmentDestination,
            TargetPathIndex = randomPathPointDestination + 1
//            TargetPathIndex = 0
        };

        if (!PathExists(destination, randomSegment))
        {
            return;
        }
        
        if (randomPathPoint == randomPathPointDestination && randomSegment == randomSegmentDestination)
        {
            //EntityManager.RemoveEntity(newCarEntity);
            return;
        }
        
        List<Destination> destinations = new List<Destination>() {destination};

        Car car = new Car()
        {
            ConnectedSegment = segment.EntityID,
            GroupID = 0,
            Position = position,
            Color = Color.LightGreen,
            Rotation = Matrix.Identity,
            Destinations = destinations,
            InitialPathIndex = randomPathPoint + 1
//            InitialPathIndex = segment.TotalPathLength - 3
        };
        car.SegmentPath = BuildCarPath(car, destination);

        car.Position = GetInitialPosition(car);
        
        int newCarEntity = EntityManager.AddEntity();
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