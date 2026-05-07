using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
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
    public const float MaxStallTime = 10.0f; //in seconds
    public static void BasicBehavior(float virtualDt)
    {
         List<int> entities = ComponentManager.GetComponent<Car>();
         List<int> entitiesToRemove = new List<int>();
         //Dictionary<int, double> carDestinationTimes = new Dictionary<int, double>();
         //List<double> finalDestinationTimes = new List<double>();
         
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
                 PathSegment destinationSegment = ComponentManager.GetEntityComponent<PathSegment>((car.Destinations.Peek().TargetSegmentID));

                 if (destinationSegment is null)
                 {
                     Console.WriteLine("Null Destination");
                     car.Destinations.Clear();
                 }
             }
             
             if (car.Destinations.Count > 0 && !PathExists(car.Destinations.Peek(), car.ConnectedSegment))
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
                 //Console.WriteLine();
                 continue;
             }

             ValidateDestinationSegment(car.Destinations.Peek(), car);
             ValidateCurrentSegment(car, entity);
             
             SimulationSuper simulationSuper = EntityManager.GetGlobalComponent<SimulationSuper>();

             // try
             // {
                MoveCar(entity, virtualDt);
             // }
             // catch (Exception e)
             // {
             //     //TODO there are still times where we are getting index errors in getnextvertex so killing those entities for now when it happens
             //     Console.WriteLine(e);
             //     entitiesToRemove.Add(entity);
             // }
             
         }

         foreach (int entity in entitiesToRemove)
         {
             //Console.WriteLine("Killing Entity: " + entity);
             Car car = ComponentManager.GetEntityComponent<Car>(entity);
             // Check if the car is currently waiting at a connector
             if (car.OnConnector != -1)
             {
                 RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
                 PathSegmentConnector connector = roadMesh.PathSegmentConnectors[car.OnConnector];
                 
                 connector.StopQueue.Remove(entity);
             }
             
             Console.WriteLine("Removing entity for some unknown reason");
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
        //Console.WriteLine($"Segment Path Current Index: {car.SegmentPath.CurrentIndex}");
        //Console.WriteLine($"Segment Size: {pathSegment.Path.Length}");
        Vector3 currentVertexPos = GetPathVertexFromIndex(car.SegmentPath.CurrentIndex, pathSegment);
        Vector3 nextVertexPos = GetPathVertexFromIndex(car.SegmentPath.CurrentIndex + car.SegmentPath.Direction, pathSegment);
        //Console.WriteLine($"CurrentVertexPos: {currentVertexPos.X}, {currentVertexPos.Y}, {currentVertexPos.Z}");
        //Console.WriteLine($"NextVertexPos: {nextVertexPos.X}, {nextVertexPos.Y}, {nextVertexPos.Z}");
        //RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        Vector3 direction = Vector3.Normalize(nextVertexPos - currentVertexPos);
        //Console.WriteLine($"Direction: {direction.X}, {direction.Y}, {direction.Z}" );
        //Console.WriteLine();
        return nextVertexPos + RoadSideOffset(direction) * car.CurrentLane;
    }

    private static void BuildIntersectionPath(Car car, Vector3 p1, Vector3 p2, Vector3 connectorPosition)
    {
        //     Vector3 dir = Vector3.Slerp(v1, v2, 0.5f); //can use Slerp to get angles between.
        Vector3 p2Dir = Vector3.Normalize(p2 - connectorPosition);
        Vector3 p1Dir = Vector3.Normalize(connectorPosition - p1);
        Vector3 p2Offset = Vector3.Cross(p2Dir, Vector3.Up);
        Vector3 p1Offset = Vector3.Cross(p1Dir, Vector3.Up);
        
        Vector3 diff = p1Dir - p2Dir;
        Vector3 tangent;

        // If diff is practically zero, the car is going straight.
        if (diff.LengthSquared() < 0.0001f) 
        {
            // For a straight line, the lane offset doesn't need a complex corner tangent.
            // It just uses the exact same lane offset it had coming in.
            tangent = -p1Offset; 
        }
        else
        {
            // The car is turning, proceed with normal corner tangent math
            tangent = Vector3.Normalize(diff);
            float turnSign = MathF.Sign(Vector3.Cross(p1Dir, p2Dir).Y);
            if (turnSign > 0)
                tangent = -tangent;
        }
        
        // bool isLeftTurn = turnSign > 0;

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

    private static int GetNextConnectorIDWithDir(int currentSegment, int dir)
    {
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(currentSegment);

        if (dir == -1)
            return (int)segment.FrontConnector;
        else
            return (int)segment.EndConnector;
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
            car.Log.Add("Target Destination Segment: " + car.Destinations.Peek().TargetSegmentID);
            car.Log.Add("Target Destination Index: " + car.Destinations.Peek().TargetPathIndex);
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

            car.SegmentPath = BuildCarPath(car, car.Destinations.Peek());
            car.Log.Add("Direction: " + car.SegmentPath.Direction);
            car.Log.Add("New Connected Segment Size: " + car.SegmentPath.SegmentSize);

            //Console.WriteLine();
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
    private static bool WaitOnTraffic(int entity, float velocity, Vector3 direction, float virtualDt)
    {
        Car car = ComponentManager.GetEntityComponent<Car>(entity);
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
    
        //Vector3 predPosition = car.Position + velocity * direction;

        //here we check if the car is "stuck" or hasn't moved in 10 seconds. This is reset every time the car moves
        //if the car is stuck in traffic, they will still creep forward slowly every once in a while so this should
        //help with complete stalls
        if (car.WaitTimer >= MaxStallTime)
        {
            car.WaitTimer = 0;
            car.StuckCooldown = 5;
            return false;
        }

        if (car.StuckCooldown > 0)
        {
            car.StuckCooldown -= virtualDt;
            return false;
        }
    
        if (car.OnConnector != -1)
        {
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[car.OnConnector];
            
            if (connector.SegmentEntities.Count > 2)
            {
                if (car.InIntersection)
                    return false;
                
                foreach (var targetCarEntity in segment.EntitiesOnSegment)
                {
                    Car targetCar = ComponentManager.GetEntityComponent<Car>(targetCarEntity);
                    if (car.SegmentPath.Direction == targetCar.SegmentPath.Direction)
                    {
                        //need to check if the next path is blocked
                        PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
                        int currentIndex = car.SegmentPath.CurrentIndex;
                        Vector3 point = connectedSegment.Path[currentIndex];

                        if (targetCar.OnConnector == -1 && SphereIntersection.WillIntersect(point, targetCar.Position))
                            return true;
                    }
                }
                return false;
            } 
        }
        
        // if (car.OnConnector == -1)
        //TODO fix this for inside turns on road connectors (not intersections)
        {
            foreach (int targetCarEntity in segment.EntitiesOnSegment) //wait on path on segment only?
            {
                Car targetCar = ComponentManager.GetEntityComponent<Car>(targetCarEntity);

                if (targetCar == null)
                    continue;
                
                if (car.OnConnector != -1 && targetCar.OnConnector != -1)
                {
                    Vector3 targetCarDir = Vector3.Normalize(targetCar.OverridePath.Peek() - targetCar.Position);
                    if (Vector3.Dot(targetCarDir, direction) < 0)
                        continue;
                }
                
                if (targetCar.SegmentPath.Direction != car.SegmentPath.Direction)
                    continue;
                if (targetCarEntity == entity)
                    continue;
                if (CarToCarWillIntersect(car, targetCar, direction, velocity))
                    return true;
            }
        }
        
        //I think we just check the next path destination to see if we are hitting a connector
        //if (car.Destinations[0].Path.Count > 0)
        if (car.Destinations.Peek().TargetPathIndex != car.ConnectedSegment)
        {
            int connectorID = GetNextConnectorIDWithDir(car.ConnectedSegment, car.SegmentPath.Direction);
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[connectorID];
    
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
    
    private static bool WaitOnStopSign(int carEntity)
    {
        Car car = ComponentManager.GetEntityComponent<Car>(carEntity);
        
        if (car.OnConnector == -1)
            return false;
        
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegmentConnector connector = roadMesh.PathSegmentConnectors[car.OnConnector];
    
        if (connector.SegmentEntities.Count <= 2)
            return false;
        
        if (connector.StopQueue.Count > 0 && carEntity == connector.StopQueue[0])
            return false;
        
        return true;
    }
   
   
    //TODO need to add collision check here for cars so they don't hit/pass through each other, especially at intersections
    private static void MoveCar(int entity, float virtualDt)
    {
        Car car = ComponentManager.GetEntityComponent<Car>(entity);
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();

        car.WaitTimer += virtualDt;

        car.TimeOnSegment += virtualDt;
        
        float remaining = 2 * virtualDt;
        while (remaining > 0.000001f)
        {
            bool isOverridden = (car.OverridePath.Count > 0);
            PathSegment connectedSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
            
           //if (WaitOnStopSign(entity))
             //break;

            //my new stuff
            if (StoplightSystems.WaitOnStopLight(entity))
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
                if (WaitOnTraffic(entity, remaining, direction, virtualDt))
                    return;
                
                car.Position += direction * remaining;
                car.Rotation = Matrix.CreateWorld(Vector3.Zero, direction, Vector3.Up);
                car.WaitTimer = 0;
                
                if (isOverridden && car.OnConnector != -1)
                {
                    PathSegmentConnector previousConnector = roadMesh.PathSegmentConnectors[car.OnConnector];
                    if (previousConnector.SegmentEntities.Count > 2)
                        car.InIntersection = true;
                }
                return;
            }

            remaining -= distanceTo;
            
            bool finalSegment = (car.connectedSegment == car.Destinations.Peek().TargetSegmentID);
            if (car.SegmentPath.CurrentIndex == car.Destinations.Peek().TargetPathIndex && finalSegment)
            {
                Destination recycledDest = car.Destinations.Dequeue();
                car.Destinations.Enqueue(recycledDest);
                SimulationSystems.IncrementDestinations(car.IsReroute);
                if (car.Destinations.Count <= 0)
                {
                    connectedSegment.EntitiesOnSegment.Remove(entity);
                }
                else
                {
                    car.InitialPathIndex = car.SegmentPath.CurrentIndex;
                    car.SegmentPath = BuildCarPath(car, car.Destinations.Peek());
                    car.Position = GetInitialPosition(car);
                }
                return;
            }
            
            // Console.WriteLine("Target Segment: " + car.Destinations[0].TargetSegmentID);
            // Console.WriteLine("Target Point Index: " + car.Destinations[0].TargetPathIndex);
            //
            // Console.WriteLine("Current Segment: " + car.connectedSegment);
            // Console.WriteLine("Current Point Index: " + car.SegmentPath.CurrentIndex);
            if (WaitOnTraffic(entity, remaining, direction, virtualDt))
                return;

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
                        connectorQueued.StopQueue.Remove(entity);
                    }
                    car.OnConnector = -1;
                    car.IgnoreYellow = false;
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
                car.TimeOnSegment = 0;
                
                connectedSegment.EntitiesOnSegment.Remove(entity);
                
                //TODO bug here, I think the lastConnectorID might be wrong for reroute paths, we have to actually check it against the current segments 2 connectors to see which one we just left or something I don't know
                int lastConnectorID = car.SegmentPath.NextConnectorID;

                //this might work
                // int lastConnectorID = (int)connectedSegment.EndConnector;
                // if (car.SegmentPath.Direction == -1)
                //     lastConnectorID = (int)connectedSegment.FrontConnector;
                //
                int nextConnectorId = GetNextConnectorID(car.Destinations.Peek(), lastConnectorID);
                //Console.WriteLine("nextConnectorID before Reroute: " + nextConnectorId);
                
                
                //my thought process for this is if I set the lastConnectorID and the Destination based on the current reroute path next point
                //then we can get a good path forward. I'm not 100% sure though, because the destination is a segment.
                //I'm wondering if I can use a "dummy" or just ignore it entirely for this portion. I'd need to double
                //check how I wrote that portion, but I don't wanna do that because I'm lazy
                
                //GetNextSegment is safe because it just checks if the car is on the current segment
                //BuildCarPath should be safe as it's only used as a check when on an initial point
                //GetNextSegment I think this is safe too, it just checks that we aren't getting the same segment we just left, I think we are good on all 3 of these
                
                //there might be some unknown bugs with this, so if I go this route, I should be willing to think in a different direction
                if (car.IsReroute)
                {
                    car.ReroutePath = RerouteSystem.Reroute(2, lastConnectorID, car.Destinations.Peek());
                } 
                
                if (car.ReroutePath is not null)
                {
                    ReroutePath reroutePath = (ReroutePath)car.ReroutePath;

                    if (reroutePath.IntermediaryNodes.Count > 1)
                    {
                        nextConnectorId = ((ReroutePath)car.ReroutePath).IntermediaryNodes[1];
                        Console.WriteLine("\nCar is rerouting");
                        Console.WriteLine("lastConnectorID: " + lastConnectorID);
                        Console.WriteLine("nextConnectorId: " + nextConnectorId);
                        Console.WriteLine("Current Segment End Connector: " + connectedSegment.EndConnector);
                        Console.WriteLine("Current Segment Front Connector: " + connectedSegment.FrontConnector);
                    }
                    
                    //Console.WriteLine("nextConnectorId: " + nextConnectorId);
                    
                }
                
                //Console.WriteLine("nextConnectorId: " + nextConnectorId);
                
                int lastPathIndex = car.SegmentPath.CurrentIndex;
                car.PreviousSegment = car.ConnectedSegment;
                int nextSegmentId = GetNextSegment(car.Destinations.Peek(), nextConnectorId, lastConnectorID);
                car.connectedSegment = nextSegmentId;
                PathSegment nextSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
                nextSegment.EntitiesOnSegment.Add(entity);
                Vector3 p1 = GetPathVertexFromIndex(car.SegmentPath.CurrentIndex, connectedSegment);
                car.SegmentPath = BuildCarPath(car, car.Destinations.Peek(), lastConnectorID, nextConnectorId);
                PathSegment newSegment = ComponentManager.GetEntityComponent<PathSegment>(car.ConnectedSegment);
                Vector3 p2 = GetPathVertexFromIndex(car.SegmentPath.CurrentIndex, newSegment);
                Vector3 p3 = roadMesh.PathSegmentConnectors[lastConnectorID].Position;
                BuildIntersectionPath(car, p1, p2, p3);
                car.Position = car.OverridePath.Pop();
                car.OnConnector = lastConnectorID;
                PathSegmentConnector lastConnector = roadMesh.PathSegmentConnectors[lastConnectorID];
                if (lastConnector.SegmentEntities.Count > 2)
                {
                    car.InIntersection = false;
                    lastConnector.StopQueue.Add(entity);
                } 
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
        if (lastConnectorID == -1 && car.Destinations.Peek().TargetSegmentID != car.connectedSegment) //new car target on different segment
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

        if (lastConnectorID == -1 && car.Destinations.Peek().TargetSegmentID == car.connectedSegment) //new car target on same segment
        {
            carPath.CurrentIndex = car.InitialPathIndex;
            if (car.InitialPathIndex > car.Destinations.Peek().TargetPathIndex)
                carPath.Direction = -1;
            else
                carPath.Direction = 1;
            carPath.NextConnectorID = -1;
            carPath.LastConnectorID = -1;
            return carPath;
        }

        if (car.Destinations.Peek().TargetSegmentID == car.connectedSegment) //from connector to final segment
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
    
    /// <summary>
    /// Gets a random path point
    /// </summary>
    /// <returns>(int SegmentEntityID, int SegmentPathPointIndex)</returns>
    public static (int, int) GetRandomPathPoint(Random random)
    {
        List<int> segments = ComponentManager.GetComponent<PathSegment>();
        int randomSegment = random.Next(segments.Count);
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(segments[randomSegment]);
        
        if (segment.Path.Length < 3)
            return (-1, -1);
        int randomPathPoint = random.Next(1, segment.Path.Length - 1); //don't let it be the last or the first point on a path, this is the start of an intersection
        
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

    public static void GenerateCars(int count = 2000, int seed = 1, float behaviorDistribution = 1)
    {
        Random random = new Random(seed);
        
        for (int i = 0; i < count * behaviorDistribution; i++)
        {
            GenerateRandomCar(random);
        }

        for (int i = 0; i < count - (count * behaviorDistribution); i++)
        {
            GenerateRandomCar(random, CarBehavior.Rerouting);
        }
    }
    
    public enum CarBehavior
    {
        Basic,
        Rerouting
    }

    public static void GenerateRandomCar(Random random, CarBehavior behavior = CarBehavior.Basic, int destinationCount = 100)
    {
        //List<int> segments = ComponentManager.GetComponent<PathSegment>();
        (int randomSegment, int randomPathPoint) = GetRandomPathPoint(random);
        if (randomSegment == -1)
            return;
        
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(randomSegment);
        Vector3 position = segment.Path[randomPathPoint];

        (int randomSegmentDestination, int randomPathPointDestination) = GetRandomPathPoint(random);
        if (randomSegmentDestination == -1)
            return;

        Destination destination = new Destination()
        {
            TargetSegmentID = randomSegmentDestination, //SegmentID = randomSegmentDestination,
            TargetPathIndex = randomPathPointDestination
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

        Queue<Destination> destinations = new Queue<Destination>();

        List<int> segmentEntities = ComponentManager.GetComponent<PathSegment>();
        if (segmentEntities.Count > 4)
        {
            int lastSegment = -1;
            while (destinations.Count < destinationCount)
            {
                (int randomSegmentDest, int randomPathPointDest) = GetRandomPathPoint(random);
                if (randomSegmentDest == lastSegment)
                    continue;
                
                destinations.Enqueue(new Destination()
                {
                    TargetSegmentID = randomSegmentDest,
                    TargetPathIndex = randomPathPointDest
                });
            }   
        }
        
        Car car = new Car()
        {
            ConnectedSegment = segment.EntityID,
            GroupID = 0,
            Position = position,
            Color = Color.LightGreen,
            Rotation = Matrix.Identity,
            Destinations = destinations,
            InitialPathIndex = randomPathPoint
//            InitialPathIndex = segment.TotalPathLength - 3
        };
        car.SegmentPath = BuildCarPath(car, destination);

        SimulationSuper simulationSuper = EntityManager.GetGlobalComponent<SimulationSuper>();

        if (behavior == CarBehavior.Rerouting)
        {
            car.Color = Color.HotPink;
            car.IsReroute = true;
        }

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