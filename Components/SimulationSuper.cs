using System.Collections.Generic;

namespace CS4620IS.Components;

public class SimulationSuper
{
    public int SimSpeed = 1;
    public float GenBehaviorDistribution;
    public int GenCarCount;
    public int GenSeed;
    public float SimTimer;
    public int SimCount;
    public float MaxSimCount;
    public float MaxSimTime;
    public float CongestionTimer = 0; //any code using this timer for a check needs to happen after simsuper is updated
    public readonly float CONGESTION_TIMER_MAX = 5;
    public int DistributionChangeValue;
    public SimulationMetrics RerouteCarMetrics = new SimulationMetrics();
    public SimulationMetrics BasicCarMetrics = new SimulationMetrics();
    public SimulationMetrics AllMetrics = new SimulationMetrics();
    public List<float> EquilibriumValues = new List<float>();
    public CurrentSimTracking CurrentSimTracking = new CurrentSimTracking();
}

public class CurrentSimTracking
{
    public List<(float, float)> AverageCongestionCollection = new List<(float, float)>();
    public float BasicToRerouteRatio;
    public int BasicDestinations = 0;
    public int RerouteDestinations = 0;
}

public class SimulationMetrics
{
    public List<List<(float, float)>> AverageCongestionCollection = new List<List<(float, float)>>();
    public List<float> DistanceTraveled = new List<float>();
    public List<float> AverageTimeToDestination = new List<float>();
    public List<int> TotalDestinationsReached = new List<int>();
}