using System;
using System.IO;
using CS4620IS.Collision;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using PlanetaryExpansion;

namespace CS4620IS;

public class CarCollisionSystems
{
    public const float MaxStallTime = 15.0f; //in seconds

    public static void BuildLeadingSegmentLists()
    {
        PathSegment[] segments = ComponentManager.GetComponents<PathSegment>();
        Car[] cars = ComponentManager.GetComponents<Car>();
        InstancedData[] instanceDatas = ComponentManager.GetComponents<InstancedData>();
        
        int segmentCid = ComponentManager.GetComponentID<PathSegment>();
        
        int segmentsCount = ComponentManager.PoolCounts[segmentCid];
        int carCounts = ComponentManager.GetCount<Car>();
        
        for (int i = 0; i < segmentsCount; i++)
        {
            PathSegment segment = segments[i];
            segment.NegativeCars.Clear();
            segment.PositiveCars.Clear();
        }

        for (int i = 0; i < carCounts; i++)
        {
            ref Car car = ref cars[i];
            if (car.OnConnector == -1)
                continue;
            
            InstancedData instanceData = instanceDatas[i];
            
            int segmentDenseId = ComponentManager.GetDenseId(car.connectedSegmentId, segmentCid);
            PathSegment segment = segments[segmentDenseId];

            float offset = Vector3.Distance(instanceData.Position, segment.Path[car.CarPath.CurrentIndex]);
            float distance = PathSegmentSystems.GetDistance(segment, car.CarPath.Direction, car.CarPath.CurrentIndex, offset);

            car.DistOnSegment = distance;
            
            if (car.CarPath.Direction == -1)
                segment.NegativeCars.Add(i);
            else
                segment.PositiveCars.Add(i);
        }
        
        for (int i = 0; i < segmentsCount; i++)
        {
            PathSegment segment = segments[i];

            if (segment.PositiveCars.Count > 1)
            {
                // The lambda captures 'cars' from the method scope
                var localCars = cars;
                segment.PositiveCars.Sort((a, b) => localCars[a].DistOnSegment.CompareTo(localCars[b].DistOnSegment));
            }

            if (segment.NegativeCars.Count > 1)
            {
                segment.NegativeCars.Sort((a, b) => cars[a].DistOnSegment.CompareTo(cars[b].DistOnSegment));
            }
        }
    }

    public static bool UpdateCollision()
    {
        return false;
    }

    //TODO WaitOnTraffic isn't complete
    //For cars that are on a connector, before they've left a connector, they should check that the road ahead isn't backed up
    //not just wait on the intersection
    private static bool WaitOnTraffic(ref Car car, ref InstancedData instanceData, int entity, float velocity, Vector3 direction, float virtualDt, Destination destination, Car[] cars, InstancedData[] instanceDatas, RoadMesh roadMesh, int carComponentId, PathSegment connectedSegment, PathSegment[] segments, int segmentComponentId)
    {
        //Vector3 predPosition = instancedData.Position + velocity * direction;

        //here we check if the car is "stuck" or hasn't moved in 10 seconds. This is reset every time the car moves
        //if the car is stuck in traffic, they will still creep forward slowly every once in a while so this should
        //help with complete stalls
        if (car.WaitTimer >= MaxStallTime)
        {
            car.WaitTimer = 0;
            car.StuckCooldown = 1f;
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
                
                foreach (var targetCarEntity in connectedSegment.EntitiesOnSegment)
                {
                    int denseId = ComponentManager.GetDenseId(targetCarEntity, carComponentId);
                    if (denseId == -1) continue;
                    ref Car targetCar = ref cars[denseId];
                    
                    //don't even go to WillIntersect, just ignore from here
                    if (MathF.Abs(targetCar.CarPath.CurrentIndex - car.CarPath.CurrentIndex) > 2)
                        continue;
                    
                    if (car.CarPath.Direction == targetCar.CarPath.Direction)
                    {
                        //need to check if the next path is blocked
                        int currentIndex = car.CarPath.CurrentIndex;
                        Vector3 point = connectedSegment.Path[currentIndex];

                        if (targetCar.OnConnector == -1 && SphereIntersection.WillIntersect(point, instanceData.Position))
                            return true;
                    }
                }
                return false;
            } 
        }
        
        // if (car.OnConnector == -1)
        //TODO fix this for inside turns on road connectors (not intersections)
        {
            foreach (int targetCarEntity in connectedSegment.EntitiesOnSegment) //wait on path on segment only?
            {
                int denseId = ComponentManager.GetDenseId(targetCarEntity, carComponentId);
                if (denseId == -1) continue;
                ref Car targetCar = ref cars[denseId];
                ref InstancedData targetInstanceData = ref instanceDatas[denseId];

                if (!targetCar.Alive)
                    continue;
                
                if (car.OnConnector != -1 && targetCar.OnConnector != -1)
                {
                    Vector3 targetCarDir = Vector3.Normalize(targetCar.OverridePath.Peek() - targetInstanceData.Position);
                    if (Vector3.Dot(targetCarDir, direction) < 0)
                        continue;
                }
                
                if (targetCar.CarPath.Direction != car.CarPath.Direction)
                    continue;
                if (targetCarEntity == entity)
                    continue;
                if (CarToCarWillIntersect(ref targetCar, ref instanceData, ref targetInstanceData, direction, velocity))
                    return true;
            }
        }
        
        //I think we just check the next path destination to see if we are hitting a connector
        //if (car.Destinations[0].Path.Count > 0)
        if (destination.TargetPathIndex != car.ConnectedSegmentId)
        {
            int connectorID = GetNextConnectorIDWithDir(car.ConnectedSegmentId, car.CarPath.Direction);
            PathSegmentConnector connector = roadMesh.PathSegmentConnectors[connectorID];
    
            if (connector.SegmentEntities.Count > 2) //check against entering intersection
            {
                foreach (int targetCarEntity in connector.StopQueue)
                {
                    int denseId = ComponentManager.GetDenseId(targetCarEntity, carComponentId);
                    if (denseId == -1) continue;
                    ref Car targetCar = ref cars[denseId];
                    ref InstancedData targetInstanceData = ref  instanceDatas[denseId];
                    // if (targetCar.SegmentPath.Direction != car.SegmentPath.Direction)
                    //     continue;
                    if (targetCarEntity == entity)
                        continue;
                    if (CarToCarWillIntersect(ref targetCar, ref instanceData, ref targetInstanceData, direction, velocity))
                        return true;
                }
            }
            
            else //check against entering connector that has only 2 segments so isn't an intersection
            {
                foreach (int nextSegmentEntity in connector.SegmentEntities)
                {
                    if (nextSegmentEntity == car.ConnectedSegmentId)
                        continue;
                    
                    int segmentDenseId =  ComponentManager.GetDenseId(nextSegmentEntity, segmentComponentId);
                    PathSegment nextSegment = segments[segmentDenseId];
                
                    foreach (int targetCarEntity in nextSegment.EntitiesOnSegment)
                    {
                        int denseId = ComponentManager.GetDenseId(targetCarEntity, carComponentId);
                        if (denseId == -1) continue;
                        ref Car targetCar = ref cars[denseId];
                        ref InstancedData targetInstanceData = ref  instanceDatas[denseId];
                        // if (targetCar.SegmentPath.Direction != car.SegmentPath.Direction)
                        //     continue;
                        if (targetCarEntity == entity)
                            continue;
            
                        if (CarToCarWillIntersect(ref targetCar, ref instanceData, ref targetInstanceData, direction, velocity))
                        {
                            return true;
                        }
                    }
                }
            }
        }
    
        return false;
    }
   
    private static int GetNextConnectorIDWithDir(int currentSegment, int dir)
    {
        PathSegment segment = ComponentManager.GetComponent<PathSegment>(currentSegment);

        if (dir == -1)
            return (int)segment.FrontConnector;
        else
            return (int)segment.EndConnector;
    }
   
    private static bool CarToCarWillIntersect(ref Car targetCar, ref InstancedData sourceInstancedData, ref InstancedData targetInstancedData, Vector3 sourceDir, float sourceVelocity)
    {
        var targetNext = targetCar.NextPathVertex;
        var targetVelocity = targetNext - targetInstancedData.Position;

        targetVelocity.Normalize();

        return SphereIntersection.WillIntersect(sourceInstancedData.Position, targetInstancedData.Position, sourceDir * sourceVelocity);
    }
}