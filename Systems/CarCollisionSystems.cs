using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CS4620IS.Collision;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using PlanetaryExpansion;
using ScottPlot.Colormaps;

namespace CS4620IS;

public class CarCollisionSystems
{
    public const float MaxStallTime = 15.0f; //in seconds
    private static Car[] _cars;
    private static InstancedData[] _instancedData;
    private static PathSegment[] _segments;
    private static RoadMesh _roadMesh;
    private static float _velocity;
    private static float _virtualDt;
    private static int _carCount;
    private static int _segmentCount;
    private static int _segmentCid;
    
    public static void BuildLeadingSegmentLists()
    {
        for (int i = 0; i < _segmentCount; i++)
        {
            PathSegment segment = _segments[i];
            segment.NegativeCars.Clear();
            segment.PositiveCars.Clear();
        }

        foreach (PathSegmentConnector connector in _roadMesh.PathSegmentConnectors)
        {
            connector.DenseCarsOnConnector.Clear();
        }

        for (int i = 0; i < _carCount; i++)
        {
            ref Car car = ref _cars[i];

            car.WaitOnTraffic = false;
            
            InstancedData instanceData = _instancedData[i];
            
            int segmentDenseId = ComponentManager.GetDenseId(car.connectedSegmentId, _segmentCid);
            PathSegment segment = _segments[segmentDenseId];

            if (car.OnConnector != -1)
            {
                PathSegmentConnector connector = _roadMesh.PathSegmentConnectors[car.OnConnector];
                connector.DenseCarsOnConnector.Add(i);
                continue;
            }

            float offset = Vector3.Distance(instanceData.Position, segment.Path[car.CarPath.CurrentIndex]);
            float distance = PathSegmentSystems.GetDistance(segment, car.CarPath.Direction, car.CarPath.CurrentIndex, offset);
            
            car.DistOnSegment = distance;
            
            if (car.CarPath.Direction == -1)
                segment.NegativeCars.Add(i);
            else
                segment.PositiveCars.Add(i);
        }
        
        for (int i = 0; i < _segmentCount; i++)
        {
            PathSegment segment = _segments[i];

            if (segment.PositiveCars.Count > 1)
            {
                segment.PositiveCars.Sort((a, b) => _cars[a].DistOnSegment.CompareTo(_cars[b].DistOnSegment));            }

            if (segment.NegativeCars.Count > 1)
            {
                segment.NegativeCars.Sort((a, b) => _cars[a].DistOnSegment.CompareTo(_cars[b].DistOnSegment));
            }
        }
    }

    public static void Update(float virtualDt)
    {
        _cars = ComponentManager.GetComponents<Car>();
        _instancedData = ComponentManager.GetComponents<InstancedData>();
        _segments = ComponentManager.GetComponents<PathSegment>();
        _roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        _velocity = virtualDt * 2; // include your buffer
        
        _segmentCid = ComponentManager.GetComponentID<PathSegment>();
        
        _segmentCount = ComponentManager.PoolCounts[_segmentCid];
        _carCount = ComponentManager.GetCount<Car>();

        _roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        _virtualDt = virtualDt;
        
        BuildLeadingSegmentLists();
        ProcessSegmentLists();
        ProcessConnectorLists();
    }

    public static void ProcessSegmentLists()
    {
        for (int i = 0; i < _segmentCount; i++)
        {
            PathSegment segment = _segments[i];
            
            ProcessSegmentList(segment.NegativeCars, (int)segment.FrontConnector);
            ProcessSegmentList(segment.PositiveCars, (int)segment.EndConnector);
        }
    }

    public static void ProcessSegmentList(List<int> denseCarList, int connectorId)
    {
        for (int i = 0; i < denseCarList.Count; i++)
        {
            int idx = denseCarList[i];

            ref Car car = ref _cars[idx];
            
            //if the car is in an actual intersection, they should ignore all collision
            // if (car.InIntersection) 
            //     continue;
            
            ref InstancedData instancedData = ref _instancedData[idx];
            
            if (car.WaitTimer >= MaxStallTime)
            {
                car.WaitTimer = 0;
                car.StuckCooldown = .2f;
                continue;
            }
            
            if (car.StuckCooldown > 0)
            {
                car.StuckCooldown -= _virtualDt;
                continue;
            }

            //check if car to car intersection is happening on a segment
            if (i < denseCarList.Count - 1)
            {
                int targetIdx = denseCarList[i + 1];
                ref Car targetCar = ref _cars[targetIdx];
                ref InstancedData targetInstancedData = ref _instancedData[targetIdx];
                
                if (CarToCarWillIntersect(ref targetCar, ref instancedData, ref targetInstancedData, car.Direction, _velocity))
                {
                    car.WaitOnTraffic = true;
                    continue;
                }
            }

            PathSegmentConnector connector = _roadMesh.PathSegmentConnectors[connectorId];
            
            //car on segment checking if they will collide with another car that is stopped at the intersection
            if (WaitOnLeadingConnector(ref car, ref instancedData, connector))
            {
                car.WaitOnTraffic = true;
            }
        }
    }

    public static void ProcessConnectorLists()
    {
        foreach (PathSegmentConnector connector in _roadMesh.PathSegmentConnectors)
        {
            ProcessConnectorList(connector);
        }
    }

    public static void ProcessConnectorList(PathSegmentConnector connector)
    {
        for (int i = 0; i < connector.DenseCarsOnConnector.Count; i++)
        {
            int idx = connector.DenseCarsOnConnector[i];
            ref Car car = ref _cars[idx];
            ref InstancedData instancedData = ref _instancedData[idx];
            
            //check if car hasnt entered the inersection (happens on the first update that they move on a connector with size > 2), and if the target segment is blocked
            if (!car.InIntersection && connector.SegmentEntities.Count > 2)
            {
                if (WaitOnBlockedSegmentIntersection(ref car))
                    continue;
            }

            if (connector.SegmentEntities.Count < 3)
            {
                if (WaitOnBlockedSegment(ref car, ref instancedData))
                    continue;
            }
            
            WaitOnSingleConnector(ref car, ref instancedData, idx, connector, i);
            
        }
    }

    public static bool WaitOnBlockedSegment(ref Car car, ref InstancedData instancedData)
    {
        int segmentId = ComponentManager.GetDenseId(car.connectedSegmentId, _segmentCid);
        PathSegment segment = _segments[segmentId];

        List<int> segmentList = segment.PositiveCars;
        if (car.CarPath.Direction == -1)
            segmentList = segment.NegativeCars;
        if (segmentList.Count > 0)
        {
            int targetIdx = segmentList[0];
            Car targetCar = _cars[targetIdx];
            InstancedData targetInstancedData = _instancedData[targetIdx];
            if (CarToCarWillIntersect(ref targetCar, ref instancedData, ref targetInstancedData, car.Direction, _velocity ))
            {
                car.WaitOnTraffic = true;
                return true;
            }
        }

        return false;
    }

    public static bool WaitOnBlockedSegmentIntersection(ref Car car)
    {
        int currentIndex = car.CarPath.CurrentIndex;
        int segmentId = ComponentManager.GetDenseId(car.connectedSegmentId, _segmentCid);
        PathSegment segment = _segments[segmentId];
        Vector3 point = segment.Path[currentIndex];

        List<int> segmentList = segment.PositiveCars;
        if (car.CarPath.Direction == -1)
            segmentList = segment.NegativeCars;
        if (segmentList.Count > 0)
        {
            int targetIdx = segmentList[0];
            InstancedData instancedData = _instancedData[targetIdx];
            if (SphereIntersection.WillIntersect(point, instancedData.Position, 1))
            {
                car.WaitOnTraffic = true;
                return true;
            }
        }
        
        return false;
    }

    private static bool WaitOnLeadingConnector(ref Car car, ref InstancedData instancedData, PathSegmentConnector connector)
    {
        foreach (int targetDenseId in connector.DenseCarsOnConnector)
        {
            ref Car targetCar = ref _cars[targetDenseId];
            
            ref InstancedData targetInstancedData = ref  _instancedData[targetDenseId];
            if (CarToCarWillIntersect(ref targetCar, ref instancedData, ref targetInstancedData,
                    car.Direction, _velocity))
            {
                return true;
            }
        }

        return false;
    }

    private static void WaitOnSingleConnector(ref Car car, ref InstancedData instancedData, int idx, PathSegmentConnector connector, int i)
    {
        for (int j = i + 1; j < connector.DenseCarsOnConnector.Count; j++)
        {
            bool sourceBlocked = false;
            bool targetBlocked = false;
            
            int targetDenseId = connector.DenseCarsOnConnector[j];
            
            //if (targetDenseId == idx)
            //    continue;
        
            ref Car targetCar = ref _cars[targetDenseId];
        
            if (car.connectedSegmentId != targetCar.connectedSegmentId)
                continue;
        
            Vector3 carOneDir = Vector3.Normalize(car.OverridePath.Peek() - instancedData.Position); 
        
            ref InstancedData targetInstancedData = ref  _instancedData[targetDenseId];
            Vector3 targetCarDir = Vector3.Normalize(targetCar.OverridePath.Peek() - targetInstancedData.Position);
        
            if (Vector3.Dot(carOneDir, targetCarDir) < 0.0f)
                continue;
        
            if (CarToCarWillIntersect(ref targetCar, ref instancedData, ref targetInstancedData,
                    carOneDir, _velocity))
            {
                sourceBlocked = true;
                car.WaitOnTraffic = true;
            }
            
            if (CarToCarWillIntersect(ref car, ref targetInstancedData,  ref instancedData,
                    targetCarDir, _velocity))
            {
                targetBlocked = true;
                targetCar.WaitOnTraffic = false;
            }

            if (targetBlocked && sourceBlocked)
            {
                car.WaitOnTraffic = false;
                return;
            }
        }
    }

    //TODO WaitOnTraffic isn't complete
    //For cars that are on a connector, before they've left a connector, they should check that the road ahead isn't backed up
    //not just wait on the intersection
    public static bool WaitOnTraffic(ref Car car, int sourceDenseId, ref InstancedData instanceData, int entity, float velocity, Vector3 direction, float virtualDt, Destination destination, Car[] cars, InstancedData[] instanceDatas, RoadMesh roadMesh, int carComponentId, PathSegment connectedSegment, PathSegment[] segments, int segmentComponentId)
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
                    ref  InstancedData targetInstancedData = ref _instancedData[denseId];
                    
                    //don't even go to WillIntersect, just ignore from here
                    if (MathF.Abs(targetCar.CarPath.CurrentIndex - car.CarPath.CurrentIndex) > 2)
                        continue;
                    
                    if (car.CarPath.Direction == targetCar.CarPath.Direction)
                    {
                        //need to check if the next path is blocked
                        int currentIndex = car.CarPath.CurrentIndex;
                        Vector3 point = connectedSegment.Path[currentIndex];

                        if (targetCar.OnConnector == -1 && SphereIntersection.WillIntersect(point, targetInstancedData.Position))
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
                foreach (int denseCar in connector.DenseCarsOnConnector)
                {
                    //int denseId = ComponentManager.GetDenseId(denseCar, carComponentId);
                    //if (denseId == -1) continue;
                    //ref Car targetCar = ref cars[denseId];
                    ref InstancedData targetInstanceData = ref  instanceDatas[denseCar];
                    ref Car targetCar =  ref cars[denseCar];
                    // if (targetCar.SegmentPath.Direction != car.SegmentPath.Direction)
                    //     continue;
                    if (denseCar == sourceDenseId)
                        continue;
                    if (CarToCarWillIntersect(ref targetCar, ref instanceData, ref targetInstanceData, direction, velocity))
                    {
                        return true;
                    }
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