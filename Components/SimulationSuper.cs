using System.Collections.Generic;

namespace CS4620IS.Components;

public class SimulationSuper
{
    public int SimSpeed = 1;
    public List<Car> ControlGroup = new List<Car>();
    public List<Car> RerouteGroup = new List<Car>();
    public List<Car> PartialGroup = new List<Car>();
}