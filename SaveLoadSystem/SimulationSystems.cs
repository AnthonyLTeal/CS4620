using CS4620IS.Components;

namespace CS4620IS;

public class SimulationSystems
{
    public static void SetSpeed(int speed)
    {
        EntityManager.GetGlobalComponent<SimulationSuper>().SimSpeed = speed;
    }
}