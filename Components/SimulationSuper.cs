using System.Collections.Generic;

namespace CS4620IS.Components;

public class SimulationSuper
{
    public bool Finished = true;
    public int SimSpeed = 1;
    public float GenBehaviorDistribution = 100;
    public int GenCarCount = 1000;
    public int GenSeed = 1;
    public float SimTimer = 0;
    public float SimRuntime = 0;
    public int SimCount = 0;
    public int StartBasicDistributionPercent = 100;
    public float MaxSimTime = 60 * 10; //in seconds
    public float CongestionTimer = 0; //any code using this timer for a check needs to happen after simsuper is updated
    public readonly float CONGESTION_TIMER_MAX = 1;
    public int DistributionChangeValue = 10;
    public bool ChartsGeneratedForCurrentBatch = false;

    public List<float> EquilibriumValues = new List<float>();

    public CurrentSimTracking CurrentSimTracking = new CurrentSimTracking();
    
    public List<int> BasicDestinationsReached = new List<int>();
    public List<int> RerouteDestinationsReached = new List<int>();
    public List<int> TotalRerouteCars = new List<int>();
    public List<int> TotalBasicCars = new List<int>();
    public List<float> RerouteDistancesTravelled = new List<float>();
    public List<float> BasicDistancesTravelled = new List<float>();
    public List<float> RerouteAverageTimesToDestinations = new List<float>();
    public List<float> BasicAverageTimesToDestinations = new List<float>();
    public List<float> DistributionRatiosBasic = new List<float>();
    public List<List<(float, float)>> AverageCongestionCollectionLines = new List<List<(float, float)>>(); //first is congestion value, second is time for the line chart
}

public class CurrentSimTracking
{
    public List<(float, float)> AverageCongestionCollection = new List<(float, float)>();
    public int BasicDestinations = 0;
    public int RerouteDestinations = 0;
    public int TotalRerouteCars = 0;
    public int TotalBasicCars = 0;
    public float RerouteDistanceTravelled = 0;
    public float BasicDistanceTravelled = 0;
    public List<float> RerouteTimesToDestinations = new List<float>();
    public List<float> BasicTimesToDestinations = new List<float>();
}