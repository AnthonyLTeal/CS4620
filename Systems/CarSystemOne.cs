using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PlanetaryExpansion.Components;

namespace CS4620IS;

public class CarSystems
{
    public void BasicBehavior(GameTime gameTime)
    {
        List<int> entities = ComponentManager.GetComponent<DrawableAsset>();
        int componentID = ComponentManager.GetComponentID<DrawableAsset>();
        foreach (int entity in entities)
        {
            DrawableAsset asset = ComponentManager.GetEntityComponent<DrawableAsset>(entity);
            
            //move forward based on gps and current path
                //recalculate path
            
            //update position based current position and next node
        }
    }
}