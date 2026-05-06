using System.Collections.Generic;

namespace CS4620IS.Components;

public class SimulationSuper
{
    public int SimSpeed = 1;
    public int BasicCarCount;
    public int RerouteCarCount;
    public int SimSeed;
    public float SimTime;
    public List<Car> ControlGroup = new List<Car>();
    public List<Car> RerouteGroup = new List<Car>();
    public SimulationMetrics RerouteCarMetrics = new SimulationMetrics();
    public SimulationMetrics BasicCarMetrics = new SimulationMetrics();
    public SimulationMetrics AllMetrics = new SimulationMetrics();
    public List<float> EquilibriumValues = new List<float>();
}

public class SimulationMetrics
{
    public List<List<(float, float)>> AverageCongestionCollection = new List<List<(float, float)>>();
    public List<float> DistanceTravelled = new List<float>();
    public List<float> AverageTimeToDestination = new List<float>();
    public List<int> TotalDestinationsReached = new List<int>();
}