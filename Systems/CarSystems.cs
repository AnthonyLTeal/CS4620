using System;
using System.Collections.Generic;
using System.Linq;
using CS4620IS.Collision;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using PlanetaryExpansion;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace CS4620IS;

public class CarSystems
{
    public static void BasicBehavior(float virtualDt)
    {
        int[] entities = ComponentManager.GetOwners<Car>();
        Car[] cars = ComponentManager.GetComponents<Car>();
        InstancedData[] instancedDatas = ComponentManager.GetComponents<InstancedData>();
        PathSegment[] segments = ComponentManager.GetComponents<PathSegment>();
        
        int carCount = ComponentManager.GetCount<Car>();
        int carComponentId = ComponentManager.GetComponentID<Car>();
        int segmentComponentId = ComponentManager.GetComponentID<PathSegment>();
        
        //int instancedDataId = ComponentManager.GetComponentID<InstancedData>();
        DestinationBlob destinationBlob = ComponentManager.GetGlobalComponent<DestinationBlob>();
        SimulationSuper simSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        
        var entitiesToRemove = new List<int>();
        //Dictionary<int, double> carDestinationTimes = new Dictionary<int, double>();
        //List<double> finalDestinationTimes = new List<double>();

        for (int i = 0;  i < carCount; i++)
        {
            ref Car car = ref cars[i];
            int entity = entities[i];
            
            //TODO look for different solution for this in the future
            //for now this should be perfectly parallel to the car entity as we are always adding these 2 together and nothing else
            //but if we want to use instancedData with other entities that don't have a car in the future, might need an archtype system or something
            ref InstancedData instancedData = ref instancedDatas[i];

            //ref InstancedData instancedData = ref instancedDatas[ComponentManager.Entities[entity][instancedDataId]];

            PathSegment connectedSegment = ComponentManager.GetComponent<PathSegment>(car.ConnectedSegmentId);

            //destroy car if segment entity is dead
            if (connectedSegment is null)
            {
                Console.WriteLine("Null Segment");
                car.Destinations.Clear();
            }
            
            if (car.Destinations.Count < 1)
            {
                entitiesToRemove.Add(entity);
                //foreach (string comment in car.Log) Console.WriteLine(comment);
                //Console.WriteLine();
                continue;
            }

            Destination destination = destinationBlob.GetDestination(car.Destinations);

            //destroy if destination entity is dead
            if (car.Destinations.Count > 0)
            {
                PathSegment destinationSegment =
                    ComponentManager.GetComponent<PathSegment>(destination.TargetSegmentID);

                if (destinationSegment is null)
                {
                    Console.WriteLine("Null Destination");
                    car.Destinations.Clear();
                }
            }

            if (car.Destinations.Count > 0 && !PathExists(destination, car.ConnectedSegmentId))
            {
                Console.WriteLine("No valid path");
                car.Destinations.Clear();
            }

            if (car.Destinations.Count < 1)
            {
                entitiesToRemove.Add(entity);
                //foreach (string comment in car.Log) Console.WriteLine(comment);
                //Console.WriteLine();
                continue;
            }

            ValidateDestinationSegment(ref destination, ref car);
            ValidateCurrentSegment(ref car, entity, destination);

            //SimulationSuper simulationSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();

            // try
            // {
            MoveCar(ref car, i, ref instancedData, entity, virtualDt, destinationBlob, simSuper, cars, instancedDatas, roadMesh, carComponentId, segments, segmentComponentId);
            // }
            // catch (Exception e)
            // {
            //     //TODO there are still times where we are getting index errors in getnextvertex so killing those entities for now when it happens
            //     Console.WriteLine(e);
            //     entitiesToRemove.Add(entity);
            // }
        }

        foreach (var entity in entitiesToRemove)
        {
            //Console.WriteLine("Killing Entity: " + entity);
            Car car = ComponentManager.GetComponent<Car>(entity);
            // Check if the car is currently waiting at a connector
            if (car.OnConnector != -1)
            {
                var connector = roadMesh.PathSegmentConnectors[car.OnConnector];

                connector.StopQueue.Remove(entity);
            }

            Console.WriteLine("Removing entity for some unknown reason");
            ComponentManager.DestroyEntity(entity);
        }
    }

    private static bool PathExists(Destination destination, int segmentEntity)
    {
        PathSegment segmentOne = ComponentManager.GetComponent<PathSegment>(destination.TargetSegmentID);
        PathSegment segmentTwo = ComponentManager.GetComponent<PathSegment>(segmentEntity);

        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();

        var destinationConnector = roadMesh.PathSegmentConnectors[(int)segmentOne.EndConnector];
        return !(destinationConnector.DPath.Prevs[(int)segmentTwo.EndConnector] == -1);
    }

    private static Vector3 GetPathVertexFromIndex(int index, PathSegment pathSegment)
    {
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        if (index == -1)
        {
            var connector = roadMesh.PathSegmentConnectors[(int)pathSegment.FrontConnector];
            return connector.Position;
        }

        if (index == pathSegment.Path.Length)
        {
            var connector = roadMesh.PathSegmentConnectors[(int)pathSegment.EndConnector];
            return connector.Position;
        }

        return pathSegment.Path[index];
    }

    private static Vector3 GetPathVertex(ref Car car)
    {
        PathSegment pathSegment = ComponentManager.GetComponent<PathSegment>(car.ConnectedSegmentId);
        //Console.WriteLine($"Segment Path Current Index: {car.CarPath.CurrentIndex}");
        //Console.WriteLine($"Segment Size: {pathSegment.Path.Length}");
        var currentVertexPos = GetPathVertexFromIndex(car.CarPath.CurrentIndex, pathSegment);
        var nextVertexPos =
            GetPathVertexFromIndex(car.CarPath.CurrentIndex + car.CarPath.Direction, pathSegment);
        //Console.WriteLine($"CurrentVertexPos: {currentVertexPos.X}, {currentVertexPos.Y}, {currentVertexPos.Z}");
        //Console.WriteLine($"NextVertexPos: {nextVertexPos.X}, {nextVertexPos.Y}, {nextVertexPos.Z}");
        //RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        var direction = Vector3.Normalize(nextVertexPos - currentVertexPos);
        //Console.WriteLine($"Direction: {direction.X}, {direction.Y}, {direction.Z}" );
        //Console.WriteLine();
        return nextVertexPos + RoadSideOffset(direction) * car.CurrentLane;
    }

    private static void BuildIntersectionPath(ref Car car, Vector3 p1, Vector3 p2, Vector3 connectorPosition)
    {
        //     Vector3 dir = Vector3.Slerp(v1, v2, 0.5f); //can use Slerp to get angles between.
        var p2Dir = Vector3.Normalize(p2 - connectorPosition);
        var p1Dir = Vector3.Normalize(connectorPosition - p1);
        var p2Offset = Vector3.Cross(p2Dir, Vector3.Up);
        var p1Offset = Vector3.Cross(p1Dir, Vector3.Up);

        var diff = p1Dir - p2Dir;
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

        // PathSegment segment = ComponentManager.GetComponent<PathSegment>(car.ConnectedSegmentId);
        //
        // bool laneIsRightSide = (car.CarPath.Direction * (int)segment.PathDirection) == -1; // if that's how you define it
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

        var position = connectorPosition + tangent * car.CurrentLane;

        if (Vector3.Distance(position, p1) > Vector3.Distance(connectorPosition, p1))
        {
            //outside
        }
        
        car.OverridePath.P3 = p2 - p2Offset * car.CurrentLane;
        car.OverridePath.P2 = position;
        car.OverridePath.P1 = p1 - p1Offset * car.CurrentLane;
        car.OverridePath.Count = 3;

        // car.OverridePath.Push(p2 - p2Offset * car.CurrentLane);
        // car.OverridePath.Push(position);
        // car.OverridePath.Push(p1 - p1Offset * car.CurrentLane);
    }

    // private static Vector3? forwardLookPoint(Car car)
    // {
    //     if (car.CarPath.CurrentIndex != car.CarPath.TopIndex &&
    //           car.CarPath.CurrentIndex != car.CarPath.BottomIndex)
    //         return null;
    //
    //     if (car.Destinations.Count == 0)
    //         return null;
    //
    //     if (car.Destinations[0].Path.Count == 0)
    // }

    private static int GetNextConnectorID(Destination destination, int lastConnectorId)
    {
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        PathSegment targetSegment = ComponentManager.GetComponent<PathSegment>(destination.TargetSegmentID);

        if (targetSegment.FrontConnector == lastConnectorId || targetSegment.EndConnector == lastConnectorId)
            return -1;

        var endConnector = roadMesh.PathSegmentConnectors[(int)targetSegment.EndConnector];
        var frontConnector = roadMesh.PathSegmentConnectors[(int)targetSegment.FrontConnector];
        
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
    public static void ValidateDestinationSegment(ref Destination destination, ref Car car)
    {
        //todo look into this maybe in the future for a weird edge case
        //the car direction can matter here, if we slice at the point of the destination, and the car is on the
        //point of intersection as well (just before or after), then it will set it to the first index of the new
        //segment always. We don't always want this, if it's negative direction, we'd want to set it to the 
        //old segment at the last point, but this might not be needed when cars go to buildings directly
        PathSegment segment = ComponentManager.GetComponent<PathSegment>(destination.TargetSegmentID);
        if (destination.TargetPathIndex >= segment.Path.Length)
        {
            //car.Log.Add("Previous Destination Segment ID: " + destination.TargetSegmentID);
            //car.Log.Add("Previous Destination Path Point: " + destination.TargetPathIndex);
            RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();

            var connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
            destination.TargetSegmentID = connector.SegmentEntities[1];
            destination.TargetPathIndex = Math.Max(destination.TargetPathIndex - segment.Path.Length - 1, 0);

            //car.Log.Add("New Destination Segment ID: " + destination.TargetSegmentID);
            //car.Log.Add("New Destination Path Point: " + destination.TargetPathIndex);
        }
    }

    public static void ValidateCurrentSegment(ref Car car, int entity, Destination destination)
    {
        PathSegment segment = ComponentManager.GetComponent<PathSegment>(car.ConnectedSegmentId);

        if (car.CarPath.SegmentSize != segment.Path.Count())
        {
            //car.Log.Add("-----------------------------------");
            //car.Log.Add("Target Destination Segment: " + destination.TargetSegmentID);
            //car.Log.Add("Target Destination Index: " + destination.TargetPathIndex);
            //car.Log.Add("Previous Connected Segment:" + car.ConnectedSegmentId);
            //car.Log.Add("Previous Connected Segment Size: " + car.CarPath.SegmentSize);
            //car.Log.Add("Previous Direction: " + car.CarPath.Direction);
            //car.Log.Add("Previous Index:" + car.CarPath.CurrentIndex);

            if (car.CarPath.Direction == -1)
            {
                if (car.CarPath.CurrentIndex > segment.Path.Length + 1)
                {
                    //car.Log.Add("Branch: 1");

                    car.InitialPathIndex = Math.Max(car.CarPath.CurrentIndex - segment.Path.Length - 2, 0);
                    RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
                    var connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
                    car.ConnectedSegmentId = connector.SegmentEntities[1];
                }

                //todo this should eventually create a intersection override as well
                else if (car.CarPath.CurrentIndex >= segment.Path.Length)
                {
                    //car.Log.Add("Branch: 2");
                    car.InitialPathIndex = segment.Path.Length - 1;
                    RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
                    var connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
                    car.ConnectedSegmentId = connector.SegmentEntities[0];
                }

                else
                {
                    //car.Log.Add("Branch: 3");
                    car.CarPath.SegmentSize = segment.Path.Length;
                    car.InitialPathIndex = car.CarPath.CurrentIndex;
                }
            }
            else
            {
                if (car.CarPath.CurrentIndex > segment.Path.Length)
                {
                    //car.Log.Add("Branch: 4");
                    car.InitialPathIndex = Math.Max(car.CarPath.CurrentIndex - segment.Path.Length - 2, 0);
                    RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
                    var connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
                    car.ConnectedSegmentId = connector.SegmentEntities[1];
                }

                //todo this should eventually create a intersection override as well
                else if (car.CarPath.CurrentIndex >= segment.Path.Length - 1)
                {
                    //car.Log.Add("Branch: 5");
                    car.InitialPathIndex = 0;
                    RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
                    var connector = roadMesh.PathSegmentConnectors[(int)segment.EndConnector];
                    car.ConnectedSegmentId = connector.SegmentEntities[1];
                }

                else
                {
                    //car.Log.Add("Branch: 6");
                    car.CarPath.SegmentSize = segment.Path.Length;
                    car.InitialPathIndex = car.CarPath.CurrentIndex;
                }
            }

            //car.Log.Add("New Car Initial Index: " + car.InitialPathIndex);
            //car.Log.Add("New Car Connected Segment:" + car.ConnectedSegmentId);
            
            //no idea why we have this here, I don't think this will work the way I think it does, but it might
            car.CarPath = BuildCarPath(entity, ref car, destination, destination);
            //car.Log.Add("Direction: " + car.CarPath.Direction);
            //car.Log.Add("New Connected Segment Size: " + car.CarPath.SegmentSize);

            //Console.WriteLine();
        }
    }

    private static bool WaitOnStopSign(int carEntity)
    {
        Car car = ComponentManager.GetComponent<Car>(carEntity);

        if (car.OnConnector == -1)
            return false;

        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        var connector = roadMesh.PathSegmentConnectors[car.OnConnector];

        if (connector.SegmentEntities.Count <= 2)
            return false;

        if (connector.StopQueue.Count > 0 && carEntity == connector.StopQueue[0])
            return false;

        return true;
    }


    //TODO need to add collision check here for cars so they don't hit/pass through each other, especially at intersections
    private static void MoveCar(ref Car car, int denseId, ref InstancedData instancedData, 
        int entity, float virtualDt, DestinationBlob destinationBlob, SimulationSuper simSuper,
        Car[] cars, InstancedData[] instancedDatas, RoadMesh roadMesh, int carComponentId, PathSegment[] segments, int segmentComponentId)
    {
        //Car car = ComponentManager.GetComponent<Car>(entity);

        car.WaitTimer += virtualDt;
        car.TimeOnSegment += virtualDt;
        car.TimeToDestination += virtualDt;

        var remaining = 2 * virtualDt;
        while (remaining > 0.000001f)
        {
            Destination destination = destinationBlob.GetDestination(car.Destinations);
            bool isOverridden = car.OverridePath.Count > 0;

            int segmentDenseId = ComponentManager.GetDenseId<PathSegment>(car.connectedSegmentId);
            PathSegment connectedSegment = segments[segmentDenseId];
            //PathSegment connectedSegment = ComponentManager.GetComponent<PathSegment>(car.ConnectedSegmentId);

            //if (WaitOnStopSign(entity))
            //break;

            //my new stuff
            if (StoplightSystems.WaitOnStopLight(ref car))
                break;

            if (car.NeedsPathVertexUpdate)
            {
                car.NextPathVertex = GetPathVertex(ref car);
                car.Direction = Vector3.Normalize(car.NextPathVertex - instancedData.Position);
                car.NeedsPathVertexUpdate = false;
            }

            var nextPoint = car.NextPathVertex;
            if (isOverridden)
                nextPoint = car.OverridePath.Peek();
            //Console.WriteLine("Car Direction " + car.CarPath.Direction);
            //Console.WriteLine("Car Current Index " + car.CarPath.CurrentIndex);

            var toTarget = nextPoint - instancedData.Position;
            var distanceTo = toTarget.Length();

            var direction = distanceTo > 0f ? toTarget / distanceTo : Vector3.Zero;

            if (distanceTo > remaining)
            {
                if (car.WaitOnTraffic)
                    return;
                // if (CarCollisionSystems.WaitOnTraffic(ref car, denseId, ref instancedData, entity, remaining, direction, virtualDt, 
                //     destination, cars, instancedDatas, roadMesh, carComponentId, connectedSegment, 
                //     segments, segmentComponentId))
                //     return;

                instancedData.Position += direction * remaining;
                instancedData.Rotation = -(float)Math.Atan2(direction.X, direction.Z);;
                car.WaitTimer = 0;
                
                SimulationSystems.IncrementDistances(car.IsReroute, remaining, simSuper);
                
                if (isOverridden && car.OnConnector != -1)
                {
                    var previousConnector = roadMesh.PathSegmentConnectors[car.OnConnector];
                    if (previousConnector.SegmentEntities.Count > 2)
                        car.InIntersection = true;
                }

                return;
            }

            remaining -= distanceTo;
            car.NeedsPathVertexUpdate = true;

            var finalSegment = car.ConnectedSegmentId == destination.TargetSegmentID;
            if (car.CarPath.CurrentIndex == destination.TargetPathIndex && finalSegment)
            {
                // Console.WriteLine($"{entity} | Old Destination Segment: {destination.TargetSegmentID}");
                // Console.WriteLine($"{entity} | Old Destination Path Index: {destination.TargetPathIndex}");
                // Console.WriteLine($"{entity} | Old Destination Pointer Count: {car.Destinations.Count}");
                // Console.WriteLine($"{entity} | Old Destination Pointer Start: {car.Destinations.Start}");
                // Console.WriteLine($"{entity} | Old Destination Pointer Current: {car.Destinations.Current}");
                
                car.Destinations.Current += 1;
                if (car.Destinations.Current >= car.Destinations.Count)
                    car.Destinations.Current = 0;

                Destination previousDestination = destination;
                destination = destinationBlob.GetDestination(car.Destinations);
                
                // Console.WriteLine($"{entity} | New Destination Segment: {destination.TargetSegmentID}");
                // Console.WriteLine($"{entity} | New Destination Path Index: {destination.TargetPathIndex}");
                // Console.WriteLine($"{entity} | New Destination Pointer Count: {car.Destinations.Count}");
                // Console.WriteLine($"{entity} | New Destination Pointer Start: {car.Destinations.Start}");
                // Console.WriteLine($"{entity} | New Destination Pointer Current: {car.Destinations.Current}");
                
                //Destination recycledDest = car.Destinations.Dequeue();
                //car.Destinations.Enqueue(recycledDest);
                SimulationSystems.IncrementDestinations(car.IsReroute, car.TimeToDestination, simSuper);
                car.TimeToDestination = 0;
                
                if (car.Destinations.Count <= 0)
                {
                    connectedSegment.EntitiesOnSegment.Remove(entity);
                }
                else
                {
                    car.InitialPathIndex = car.CarPath.CurrentIndex;
                    car.CarPath = BuildCarPath(entity, ref car, previousDestination, destination);
                    //instancedData.Position = GetInitialPosition(car);
                }
                
                return;
            }

            // Console.WriteLine("Target Segment: " + car.Destinations[0].TargetSegmentID);
            // Console.WriteLine("Target Point Index: " + car.Destinations[0].TargetPathIndex);
            //
            // Console.WriteLine("Current Segment: " + car.ConnectedSegmentId);
            // Console.WriteLine("Current Point Index: " + car.CarPath.CurrentIndex);
            //if (WaitOnTraffic(entity, remaining, direction, virtualDt, destination))
            
            if (car.WaitOnTraffic)
                return;
            
            // if (CarCollisionSystems.WaitOnTraffic(ref car, denseId, ref instancedData, entity, remaining, direction, virtualDt, 
            //         destination, cars, instancedDatas, roadMesh, carComponentId, connectedSegment, 
            //         segments, segmentComponentId))
            //     return;

            if (isOverridden) //if overidden, car is traveling through an intersection/connector, but not necessarily an intersection
            {
                instancedData.Position = car.OverridePath.Pop();
                if (car.OverridePath.Count > 0)
                    continue;
                //instancedData.Position = GetPathVertex(car);
                //car.CarPath.CurrentIndex += car.CarPath.Direction; //we will want to skip the first segment point
                if (car.OverridePath.Count == 0 && car.OnConnector != -1)
                {
                    var connectorQueued = roadMesh.PathSegmentConnectors[car.OnConnector];
                    if (connectorQueued.SegmentEntities.Count() > 2) connectorQueued.StopQueue.Remove(entity);
                    car.OnConnector = -1;
                    car.IgnoreYellow = false;
                }
            }
            else
            {
                car.CarPath.CurrentIndex += car.CarPath.Direction;
                instancedData.Position = nextPoint;
            }

            if ((car.CarPath.CurrentIndex == 0 && car.CarPath.Direction == -1) ||
                (car.CarPath.CurrentIndex == car.CarPath.SegmentSize - 1 && car.CarPath.Direction == 1))
            {
                car.TimeOnSegment = 0;

                connectedSegment.EntitiesOnSegment.Remove(entity);

                //TODO bug here, I think the lastConnectorID might be wrong for reroute paths, we have to actually check it against the current segments 2 connectors to see which one we just left or something I don't know
                //int lastConnectorID = car.CarPath.NextConnectorID;

                //this might work
                int lastConnectorID = (int)connectedSegment.EndConnector;
                if (car.CarPath.Direction == -1)
                    lastConnectorID = (int)connectedSegment.FrontConnector;
                
                var nextConnectorId = GetNextConnectorID(destination, lastConnectorID);
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
                    car.ReroutePath = RerouteSystem.Reroute(3, lastConnectorID, destination);

                // if (car.ReroutePath is not null)
                if (car.ReroutePath.IsRerouting)
                {
                    nextConnectorId = car.ReroutePath.NextNode;
                    // var reroutePath = (ReroutePath)car.ReroutePath;
                    //
                    // if (reroutePath.IntermediaryNodes.Count > 1)
                    // {
                    //     nextConnectorId = ((ReroutePath)car.ReroutePath).IntermediaryNodes[1];
                    //     Console.WriteLine("\nCar is rerouting");
                    //     Console.WriteLine("lastConnectorID: " + lastConnectorID);
                    //     Console.WriteLine("nextConnectorId: " + nextConnectorId);
                    //     Console.WriteLine("Current Segment End Connector: " + connectedSegment.EndConnector);
                    //     Console.WriteLine("Current Segment Front Connector: " + connectedSegment.FrontConnector);
                    // }

                    //Console.WriteLine("nextConnectorId: " + nextConnectorId);
                }

                //Console.WriteLine("nextConnectorId: " + nextConnectorId);

                int lastPathIndex = car.CarPath.CurrentIndex;
                car.PreviousSegment = car.ConnectedSegmentId;
                var nextSegmentId = GetNextSegment(destination, nextConnectorId, lastConnectorID);
                car.ConnectedSegmentId = nextSegmentId;
                PathSegment nextSegment = ComponentManager.GetComponent<PathSegment>(car.ConnectedSegmentId);
                nextSegment.EntitiesOnSegment.Add(entity);
                var p1 = GetPathVertexFromIndex(car.CarPath.CurrentIndex, connectedSegment);
                
                //this one will never use the previous destination so we can safely pass both here, but might be
                //easier to follow if this gets updated. Basically when we get here, lastConnectorId should never be = -1
                //so when creating the new path, we will never trigger the if function that looks at the "previous" destination
                car.CarPath = BuildCarPath(entity, ref car, destination, destination, lastConnectorID, nextConnectorId);
                PathSegment newSegment = ComponentManager.GetComponent<PathSegment>(car.ConnectedSegmentId);
                var p2 = GetPathVertexFromIndex(car.CarPath.CurrentIndex, newSegment);
                var p3 = roadMesh.PathSegmentConnectors[lastConnectorID].Position;
                BuildIntersectionPath(ref car, p1, p2, p3);
                instancedData.Position = car.OverridePath.Pop();
                car.OnConnector = lastConnectorID;
                var lastConnector = roadMesh.PathSegmentConnectors[lastConnectorID];
                if (lastConnector.SegmentEntities.Count > 2)
                {
                    car.InIntersection = false;
                    lastConnector.StopQueue.Add(entity);
                    lastConnector.DenseCarsOnConnector.Add(denseId);
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

    public static int GetNextSegment(Destination destination, int nextConnectorID, int lastConnectorID)
    {
        PathSegmentConnector connector =
            ComponentManager.GetGlobalComponent<RoadMesh>().PathSegmentConnectors[lastConnectorID];
        foreach (var segmentEntity in connector.SegmentEntities)
        {
            if (segmentEntity == destination.TargetSegmentID)
                return segmentEntity;

            PathSegment segment = ComponentManager.GetComponent<PathSegment>(segmentEntity);
            if ((segment.EndConnector == nextConnectorID && segment.FrontConnector == lastConnectorID) ||
                (segment.EndConnector == lastConnectorID && segment.FrontConnector == nextConnectorID))
                return segmentEntity;
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

    private static CarPath BuildCarPath(int entity, ref Car car, Destination previousDestination, Destination nextDestination, int lastConnectorID = -1,
        int nextConnectorID = -1)
    {
        //Console.WriteLine($"Entity {entity} | Updating Car Path");
        var carPath = new CarPath();
        PathSegment connectedSegment = ComponentManager.GetComponent<PathSegment>(car.ConnectedSegmentId);
        carPath.SegmentSize = connectedSegment.Path.Length;

        //Console.WriteLine("Next Connector: " + nextConnectorID);

        int nextConnector;
        if (lastConnectorID == -1 &&
            nextDestination.TargetSegmentID != car.ConnectedSegmentId) //new car target on different segment
        {
            nextConnector = GetInitialConnector(connectedSegment, previousDestination);
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

        if (lastConnectorID == -1 &&
            nextDestination.TargetSegmentID == car.ConnectedSegmentId) //new car target on same segment
        {
            carPath.CurrentIndex = car.InitialPathIndex;
            if (car.InitialPathIndex > nextDestination.TargetPathIndex)
                carPath.Direction = -1;
            else
                carPath.Direction = 1;
            carPath.NextConnectorID = -1;
            carPath.LastConnectorID = -1;
            return carPath;
        }

        if (nextDestination.TargetSegmentID == car.ConnectedSegmentId) //from connector to final segment
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
        //List<int> segments = ComponentManager.GetComponent<PathSegment>();
        int segmentCount = ComponentManager.GetCount<PathSegment>();
        //PathSegment[] segments = ComponentManager.GetComponents<PathSegment>();
        int[] segmentOwners = ComponentManager.GetOwners<PathSegment>();
        var randomSegment = random.Next(segmentCount);
        PathSegment segment = ComponentManager.GetComponent<PathSegment>(segmentOwners[randomSegment]);

        if (segment.Path.Length < 3)
            return (-1, -1);
        var randomPathPoint =
            random.Next(1,
                segment.Path.Length -
                1); //don't let it be the last or the first point on a path, this is the start of an intersection

        return (segment.EntityID, randomPathPoint);
    }

    public static int GetInitialConnector(PathSegment currentSegment, Destination destination)
    {
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        PathSegment destSegment = ComponentManager.GetComponent<PathSegment>(destination.TargetSegmentID);

        //just added these lines for readability
        var destFront = (int)destSegment.FrontConnector;
        var destEnd = (int)destSegment.EndConnector;
        var front = (int)currentSegment.FrontConnector;
        var end = (int)currentSegment.EndConnector;

        var destFrontConnector = roadMesh.PathSegmentConnectors[destFront];
        var destEndConnector = roadMesh.PathSegmentConnectors[destEnd];
        var frontConnector = roadMesh.PathSegmentConnectors[front];
        var endConnector = roadMesh.PathSegmentConnectors[end];

        var shortestConnector = end;
        var shortestDestConnector = destEnd;

        var shortestDistance = destEndConnector.DPath.Distances[shortestConnector];

        if (shortestDistance == -1)
            return -1;

        var frontToEndDist = destEndConnector.DPath.Distances[front];
        if (frontToEndDist < shortestDistance)
        {
            shortestDistance = frontToEndDist;
            shortestConnector = front;
        }

        var endToFront = destFrontConnector.DPath.Distances[end];
        if (endToFront < shortestDistance)
        {
            shortestDistance = endToFront;
            shortestConnector = end;
            shortestDestConnector = destFront;
        }

        var frontToFront = destFrontConnector.DPath.Distances[front];
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
        PathSegment pathSegment = ComponentManager.GetComponent<PathSegment>(car.ConnectedSegmentId);
        var currentVertexPos = GetPathVertexFromIndex(car.CarPath.CurrentIndex, pathSegment);
        ;
        var nextVertexPos =
            GetPathVertexFromIndex(car.CarPath.CurrentIndex + car.CarPath.Direction, pathSegment);
        //RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        var direction = Vector3.Normalize(nextVertexPos - currentVertexPos);
        return currentVertexPos + RoadSideOffset(direction) * car.CurrentLane;
    }

    public static void GenerateCars(int count = 2000, int seed = 1, float behaviorDistribution = 1)
    {
        DestinationBlob destinationBlob = ComponentManager.GetGlobalComponent<DestinationBlob>();
        
        Random random = new Random(seed);

        for (int i = 0; i < count * behaviorDistribution; i++) 
            GenerateRandomCar(random, destinationBlob);

        for (int i = 0; i < count - count * behaviorDistribution; i++) 
            GenerateRandomCar(random, destinationBlob, CarBehavior.Rerouting);
    }

    public enum CarBehavior
    {
        Basic,
        Rerouting
    }

    public static void GenerateRandomCar(Random random, DestinationBlob destinationBlob, CarBehavior behavior = CarBehavior.Basic,
        int destinationCount = 20)
    {
        //List<int> segments = ComponentManager.GetComponent<PathSegment>();
        (int randomSegment, int randomPathPoint) = GetRandomPathPoint(random);
        if (randomSegment == -1)
            return;

        PathSegment segment = ComponentManager.GetComponent<PathSegment>(randomSegment);
        Vector3 position = segment.Path[randomPathPoint];

        (int randomSegmentDestination, int randomPathPointDestination) = GetRandomPathPoint(random);
        if (randomSegmentDestination == -1)
            return;

        Destination destination = new Destination
        {
            TargetSegmentID = randomSegmentDestination, //SegmentID = randomSegmentDestination,
            TargetPathIndex = randomPathPointDestination
//            TargetPathIndex = 0
        };

        if (!PathExists(destination, randomSegment)) return;

        if (randomPathPoint == randomPathPointDestination && randomSegment == randomSegmentDestination)
            //ComponentManager.RemoveEntity(newCarEntity);
            return;
        
        //var destinations = new Queue<Destination>();
        int start = destinationBlob.Count;
        int count = 0;
        int current = 0;

        //List<int> segmentEntities = ComponentManager.GetComponent<PathSegment>();
        int segmentCount = ComponentManager.GetCount<PathSegment>();
        if (segmentCount > 4)
        {
            var lastSegment = -1;
            while (count < destinationCount)
            {
                var (randomSegmentDest, randomPathPointDest) = GetRandomPathPoint(random);
                if (randomSegmentDest == lastSegment)
                    continue;

                count += 1;
                destinationBlob.AddDestination(new Destination
                {
                    TargetSegmentID = randomSegmentDest,
                    TargetPathIndex = randomPathPointDest
                });
            }
        }

        DestinationPointer destinations = new DestinationPointer()
        {
            Count = count,
            Current = current,
            Start = start
        };

        int newCarEntity = ComponentManager.AddEntity();
        
        var car = new Car
        {
            ConnectedSegmentId = segment.EntityID,
            Position = position,
            Color = Color.LightGreen,
            Destinations = destinations,
            InitialPathIndex = randomPathPoint,
            Scale = 0.25f,
            OnConnector = -1,
            CurrentLane = 1 * -0.2f,
            Alive = true
//            InitialPathIndex = segment.TotalPathLength - 3
        };

        var instancedData = new InstancedData()
        {
            Position = position,
            Scale = 0.25f,
            Color = Color.LightGreen
        };
        
        car.CarPath = BuildCarPath(newCarEntity, ref car, destination, destinationBlob.GetDestination(car.Destinations));

        car.NextPathVertex = GetPathVertex(ref car);
        car.Direction = Vector3.Normalize(car.NextPathVertex - instancedData.Position);
        
        // Console.WriteLine("CarPath Next Connector: " + car.CarPath.NextConnectorID);
        // Console.WriteLine("CarPath Current Index: " + car.CarPath.CurrentIndex);
        // Console.WriteLine("CarPath Direction: " + car.CarPath.Direction);
        // Console.WriteLine("CarPath Last Connector Id: " + car.CarPath.LastConnectorID);
        // Console.WriteLine("CarPath Segment Size: " + car.CarPath.SegmentSize);

        if (behavior == CarBehavior.Rerouting)
        {
            instancedData.Color = Color.HotPink;
            car.IsReroute = true;
        }
        
        SimulationSuper simulationSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();
        if (car.IsReroute)
            simulationSuper.CurrentSimTracking.TotalRerouteCars += 1;
        else
            simulationSuper.CurrentSimTracking.TotalBasicCars += 1;

        instancedData.Position = GetInitialPosition(car);

        ComponentManager.AddComponent(newCarEntity, car);
        ComponentManager.AddComponent(newCarEntity, instancedData);

        //might be better to add these to a list to be added when there is free space on the road rather than just not spawning them
        //let's see if this even works though lol
        // if (WaitOnTraffic(newCarEntity, 0, Vector3.Zero))
        // {
        //     ComponentManager.RemoveEntity(newCarEntity);
        //     return;
        // }

        segment.EntitiesOnSegment.Add(newCarEntity);
    }
}