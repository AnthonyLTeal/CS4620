using System;
using System.Collections.Generic;
using System.Linq;
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
        DestinationBlob destinationBlob  = ComponentManager.GetGlobalComponent<DestinationBlob>();
        destinationBlob.Count = 0;
        
        PathSegment[] segments =  ComponentManager.GetComponents<PathSegment>();
        int pathSegmentCount = ComponentManager.GetCount<PathSegment>();
        for (int i = pathSegmentCount - 1; i >= 0; i--)
        {
            PathSegment segment = segments[i];
            segment.EntitiesOnSegment.Clear();
            segment.SquaredTimes.Clear();
            for (int j = 0; j < 10; j++) { segment.SquaredTimes.Enqueue(0.0f);}
        }
        
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();

        foreach (PathSegmentConnector connector in roadMesh.PathSegmentConnectors)
        {
            connector.StopQueue.Clear();
        }
        
        int[] carOwners = ComponentManager.GetOwners<Car>();
        int carCount = ComponentManager.GetCount<Car>();
        
        for (int i = carCount - 1; i >= 0; i--)
        {
            ComponentManager.DestroyEntity(carOwners[i]);
        }
    }
    
//     public List<int> BasicDestinationsReached = new List<int>();
//     public List<int> RerouteDestinationsReached = new List<int>();
//     public List<int> TotalRerouteCars = new List<int>();
//     public List<int> TotalBasicCars = new List<int>();
//     public List<float> RerouteDistancesTravelled = new List<float>();
//     public List<float> BasicDistancesTravelled = new List<float>();
    // public List<float> RerouteAverageTimesToDestinations = new List<float>();
    // public List<float> BasicAverageTimesToDestinations = new List<float>();
    // public List<List<(float, float)>> AverageCongestionCollectionLines = new List<List<(float, float)>>(); //first is congestion value, second is time for the line chart
// }
//
// public class CurrentSimTracking
//     public List<(float, float)> AverageCongestionCollection = new List<(float, float)>();
//     public int BasicDestinations = 0;
//     public int RerouteDestinations = 0;
//     public int TotalRerouteCars = 0;
//     public int TotalBasicCars = 0;
//     public float RerouteDistanceTravelled = 0;
//     public float BasicDistanceTravelled = 0;
//     public List<float> RerouteTimesToDestinations = new List<float>();
//     public List<float> BasicTimesToDestinations = new List<float>();

    public static void CaptureMetrics()
    {
        SimulationSuper simSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();
        CurrentSimTracking currentSimTracking = simSuper.CurrentSimTracking;
        
        simSuper.AverageCongestionCollectionLines.Add(new List<(float, float)>(currentSimTracking.AverageCongestionCollection));
        simSuper.BasicDestinationsReached.Add(currentSimTracking.BasicDestinations);
        simSuper.RerouteDestinationsReached.Add(currentSimTracking.RerouteDestinations);
        simSuper.TotalRerouteCars.Add(currentSimTracking.TotalRerouteCars);
        simSuper.TotalBasicCars.Add(currentSimTracking.TotalBasicCars);
        simSuper.RerouteDistancesTravelled.Add(currentSimTracking.RerouteDistanceTravelled);
        simSuper.BasicDistancesTravelled.Add(currentSimTracking.BasicDistanceTravelled);

        float totalRerouteDestinationTime = 0;
        foreach (var VARIABLE in currentSimTracking.RerouteTimesToDestinations)
        {
            totalRerouteDestinationTime += VARIABLE;
        }
        simSuper.RerouteAverageTimesToDestinations.Add(totalRerouteDestinationTime/currentSimTracking.RerouteDestinations);
        
        float totalBasicDestinationTime = 0;
        foreach (var VARIABLE in currentSimTracking.BasicTimesToDestinations)
        {
            totalBasicDestinationTime += VARIABLE;
        }
        simSuper.BasicAverageTimesToDestinations.Add(totalBasicDestinationTime/currentSimTracking.BasicDestinations);
    }

    //used AI for this boilerplate, don't really care to write this all out it's silly
    public static void PrintAllSimulationRuns()
    {
        SimulationSuper simulationSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();
        // Assuming all lists are the same length, we use the count of one as our limit
        int runCount = simulationSuper.SimCount;

        for (int i = 0; i < runCount; i++)
        {
            Console.WriteLine($"\n==================================================");
            Console.WriteLine($"       SIMULATION RUN #{i + 1} SUMMARY");
            Console.WriteLine($"==================================================");

            // Header for the table
            Console.WriteLine($"{"Metric",-30} | {"Basic",-10} | {"Reroute",-10}");
            Console.WriteLine(new string('-', 55));

            // 1. Throughput
            PrintRow("Destinations Reached (Higher is better)", 
                simulationSuper.BasicDestinationsReached[i], 
                simulationSuper.RerouteDestinationsReached[i]);

            PrintRow("Total Cars", 
                simulationSuper.TotalBasicCars[i], 
                simulationSuper.TotalRerouteCars[i]);

            // 2. Performance (Distances)
            PrintRow("Distance Travelled (Higher is better)", 
                simulationSuper.BasicDistancesTravelled[i], 
                simulationSuper.RerouteDistancesTravelled[i], "f2");

            // 3. Efficiency (Time)
            PrintRow("Avg Time to Dest (Lower is better)", 
                simulationSuper.BasicAverageTimesToDestinations[i], 
                simulationSuper.RerouteAverageTimesToDestinations[i], "f2");

            // Summary Delta (Quick "Which was better?" calculation)
            float timeSaved = simulationSuper.BasicAverageTimesToDestinations[i] - simulationSuper.RerouteAverageTimesToDestinations[i];
            string winner = timeSaved > 0 ? "Reroute System" : "Basic System";
            
            Console.WriteLine(new string('-', 55));
            Console.WriteLine($"Efficiency Gain: {timeSaved:f2} sec faster with {winner}");
            Console.WriteLine($"==================================================\n");
        }
    }

    // Helper method to keep the table formatting tidy
    private static void PrintRow(string label, object basicVal, object rerouteVal, string format = "")
    {
        string basicStr = string.IsNullOrEmpty(format) ? basicVal.ToString() : string.Format("{0:" + format + "}", basicVal);
        string rerouteStr = string.IsNullOrEmpty(format) ? rerouteVal.ToString() : string.Format("{0:" + format + "}", rerouteVal);

        Console.WriteLine($"{label,-30} | {basicStr,-10} | {rerouteStr,-10}");
    }

    public static void StartBatch()
    {
            SimulationSuper simSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();

            // 1. Reset the "Global" state
            simSuper.Finished = false;
            simSuper.SimCount = 1;
            simSuper.SimTimer = 0;
            simSuper.SimRuntime = 0;
            simSuper.GenBehaviorDistribution = 100; // Starting value
            
            simSuper.BasicDestinationsReached.Clear();
            simSuper.RerouteDestinationsReached.Clear();
            simSuper.TotalRerouteCars.Clear();
            simSuper.TotalBasicCars.Clear();
            simSuper.RerouteDistancesTravelled.Clear();
            simSuper.BasicDistancesTravelled.Clear();
            simSuper.RerouteAverageTimesToDestinations.Clear();
            simSuper.BasicAverageTimesToDestinations.Clear();
            simSuper.AverageCongestionCollectionLines.Clear();

            simSuper.CurrentSimTracking = new CurrentSimTracking();

            DestroyCars(); // Just in case
            CarSystems.GenerateCars(simSuper.GenCarCount, simSuper.GenSeed, simSuper.GenBehaviorDistribution * 0.01f);

            Console.WriteLine("Batch Simulation Started...");
    }
    
    //not doing this one defensively for now, just only use 0 or 100, and the startingDistribution should be
    //evenly divisible by the distributionChange
    public static void RunSimulation(float virutalDt)
    {
        SimulationSuper simSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();

        if (simSuper.Finished)
            return;

        simSuper.CongestionTimer += virutalDt;
        simSuper.SimTimer += virutalDt;
        simSuper.SimRuntime += virutalDt;
        
        Console.Write("\rSimulation Running Time (seconds): " + simSuper.SimRuntime);

        if (simSuper.CongestionTimer > simSuper.CONGESTION_TIMER_MAX)
        {
            CaptureCurrentAverageCongestion();
            simSuper.CongestionTimer = 0;
        }

        if (simSuper.SimTimer < simSuper.MaxSimTime)
            return;
        
        CaptureMetrics();
        
        if (simSuper.GenBehaviorDistribution < 0.001f)
        {
            simSuper.Finished = true;
            PrintAllSimulationRuns();
            return;
        }
        
        simSuper.SimCount += 1;
        simSuper.GenBehaviorDistribution -= simSuper.DistributionChangeValue;
        simSuper.SimTimer = 0; // RESET the timer for the next run
        simSuper.CurrentSimTracking = new CurrentSimTracking();

        DestroyCars();
        CarSystems.GenerateCars(simSuper.GenCarCount, simSuper.GenSeed, simSuper.GenBehaviorDistribution * 0.01f);
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

    public static void IncrementDistances(bool isReroute, float distance, SimulationSuper simSuper)
    {
        if (isReroute)
            simSuper.CurrentSimTracking.RerouteDistanceTravelled += distance;
        else 
            simSuper.CurrentSimTracking.BasicDistanceTravelled += distance;
    }

    public static void IncrementDestinations(bool isReroute, float timeToDestination, SimulationSuper simSuper)
    {
        if (isReroute)
        {
            simSuper.CurrentSimTracking.RerouteTimesToDestinations.Add(timeToDestination);
            simSuper.CurrentSimTracking.RerouteDestinations += 1;
        }
        else
        {
            simSuper.CurrentSimTracking.BasicTimesToDestinations.Add(timeToDestination);
            simSuper.CurrentSimTracking.BasicDestinations += 1;
        }
    }
}