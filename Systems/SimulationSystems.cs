using System.Collections.Generic;
using CS4620IS.Components;
using PlanetaryExpansion;

namespace CS4620IS;

public class SimulationSystems
{
    public static void SetSpeed(int speed)
    {
        ComponentManager.GetGlobalComponent<SimulationSuper>().SimSpeed = speed;
    }

    public static void Clear()
    {
        DestroyCars();
        
        int[] pathSegmentOwners = ComponentManager.GetOwners<PathSegment>();
        int pathSegmentCount = ComponentManager.GetCount<PathSegment>();
        
        for (int i = pathSegmentCount - 1; i >= 0; i--)
        {
            ComponentManager.DestroyEntity(pathSegmentOwners[i]);
        }

        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        roadMesh.DestroyAll();
        
        ComponentManager.AddComponentToGlobalEntity(new SimulationSuper());
    }

    public static void DestroyCars()
    {
        int[] carOwners = ComponentManager.GetOwners<Car>();
        int carCount = ComponentManager.GetCount<Car>();
        
        for (int i = carCount - 1; i >= 0; i--)
        {
            ComponentManager.DestroyEntity(carOwners[i]);
        }
        
        PathSegment[] segments =  ComponentManager.GetComponents<PathSegment>();
        int pathSegmentCount = ComponentManager.GetCount<PathSegment>();
        for (int i = pathSegmentCount - 1; i >= 0; i--)
        {
            PathSegment segment = segments[i];
            segment.EntitiesOnSegment = new List<int>();
        }
        
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();

        foreach (PathSegmentConnector connector in roadMesh.PathSegmentConnectors)
        {
            connector.StopQueue = new List<int>();
        }
    }

    public static void RunSimulation(float virutalDt)
    {
        SimulationSuper simSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();

        simSuper.CongestionTimer += virutalDt;
        simSuper.SimTimer += virutalDt;

        if (simSuper.CongestionTimer > simSuper.CONGESTION_TIMER_MAX)
        {
            CaptureCurrentAverageCongestion();
        }
        
        if (simSuper.SimTimer < simSuper.MaxSimTime ||  simSuper.SimCount > simSuper.MaxSimCount)
            return;

        simSuper.SimTimer = 0;
        simSuper.SimCount += 1;
        simSuper.GenBehaviorDistribution += simSuper.DistributionChangeValue;
        
        //create new simulation metrics object and fill lists
        
        //clear cars next
        DestroyCars();
        
        //gen new cars with same config
        CarSystems.GenerateCars(simSuper.GenCarCount, simSuper.GenSeed, simSuper.GenBehaviorDistribution);
    }

    public static void CaptureCurrentAverageCongestion()
    {
        SimulationSuper simSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();
        
        PathSegment[] segments = ComponentManager.GetComponents<PathSegment>();
        int pathSegmentCount = ComponentManager.GetCount<PathSegment>();

        float totalCongestion = 0;
        
        for (int i = 0; i < pathSegmentCount; i++)
        {
            PathSegment segment =  segments[i];
            totalCongestion += segment.CongestionCost;
        }
        
        simSuper.CurrentSimTracking.AverageCongestionCollection.Add((totalCongestion / pathSegmentCount, simSuper.SimTimer));
    }

    public static void IncrementDestinations(bool isReroute)
    {
        SimulationSuper simSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();

        if (isReroute)
            simSuper.CurrentSimTracking.RerouteDestinations += 1;
        else
            simSuper.CurrentSimTracking.BasicDestinations += 1;
    }
}