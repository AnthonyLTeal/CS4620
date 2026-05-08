using System.Collections.Generic;
using CS4620IS.Components;
using PlanetaryExpansion;

namespace CS4620IS;

public class SimulationSystems
{
    public static void SetSpeed(int speed)
    {
        EntityManager.GetGlobalComponent<SimulationSuper>().SimSpeed = speed;
    }

    public static void Clear()
    {
        List<int> carComponents = ComponentManager.GetComponents<Car>();
        for (int i = carComponents.Count - 1; i >= 0; i--)
        {
            EntityManager.RemoveEntity(carComponents[i]);
        }
        
        List<int> pathSegmentComponents = ComponentManager.GetComponents<PathSegment>();
        for (int i = pathSegmentComponents.Count - 1; i >= 0; i--)
        {
            EntityManager.RemoveEntity(pathSegmentComponents[i]);
        }

        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        roadMesh.DestroyAll();
        
        EntityManager.AddComponentToGlobalEntity(new SimulationSuper());
    }

    public static void RunSimulation(float virutalDt)
    {
        SimulationSuper simSuper = EntityManager.GetGlobalComponent<SimulationSuper>();

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
        List<int> carComponents = ComponentManager.GetComponents<Car>();
        for (int i = carComponents.Count - 1; i >= 0; i--)
        {
            EntityManager.RemoveEntity(carComponents[i]);
        }
        
        //gen new cars with same config
        CarSystems.GenerateCars(simSuper.GenCarCount, simSuper.GenSeed, simSuper.GenBehaviorDistribution);
    }

    public static void CaptureCurrentAverageCongestion()
    {
        SimulationSuper simSuper = EntityManager.GetGlobalComponent<SimulationSuper>();
        List<int> segmentEntities = ComponentManager.GetComponents<PathSegment>();

        float totalCongestion = 0;
        
        foreach (int segmentEntity in segmentEntities)
        {
            PathSegment segment = ComponentManager.GetComponent<PathSegment>(segmentEntity);
            totalCongestion += segment.CongestionCost;
        }
        
        simSuper.CurrentSimTracking.AverageCongestionCollection.Add((totalCongestion / segmentEntities.Count, simSuper.SimTimer));
    }

    public static void IncrementDestinations(bool isReroute)
    {
        SimulationSuper simSuper = EntityManager.GetGlobalComponent<SimulationSuper>();

        if (isReroute)
            simSuper.CurrentSimTracking.RerouteDestinations += 1;
        else
            simSuper.CurrentSimTracking.BasicDestinations += 1;
    }
}