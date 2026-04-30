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
        List<int> carComponents = ComponentManager.GetComponent<Car>();
            for (int i = carComponents.Count - 1; i >= 0; i--)
            {
                EntityManager.RemoveEntity(carComponents[i]);
            }
            
            List<int> pathSegmentComponents = ComponentManager.GetComponent<PathSegment>();
            for (int i = pathSegmentComponents.Count - 1; i >= 0; i--)
            {
                EntityManager.RemoveEntity(pathSegmentComponents[i]);
            }

            RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
            roadMesh.DestroyAll();
    }
}