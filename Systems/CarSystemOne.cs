using System.Collections.Generic;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlanetaryExpansion;

namespace CS4620IS;

public class CarSystems
{
    public void BasicBehavior(GameTime gameTime)
    {
        // List<int> entities = ComponentManager.GetComponent<DrawableAsset>();
        // foreach (int entity in entities)
        // {
            // DrawableAsset asset = ComponentManager.GetEntityComponent<DrawableAsset>(entity);
            
            //move forward based on gps and current path
                //recalculate path
            
            //update position based current position and next node
        // }
    }

    public void DrawCar()
    {
        GraphicsDevice graphicsDevice = EntityManager.GetGlobalComponent<GraphicsDevice>();
    }

    public void GenerateRandomCar()
    {
        int newCarEntity = EntityManager.AddEntity();

        List<int> segments = ComponentManager.GetComponent<PathSegment>();
        int randomSegment = AOneMath.random.Next(segments.Count);
        PathSegment segment = ComponentManager.GetEntityComponent<PathSegment>(randomSegment);

        int randomPathPoint = AOneMath.random.Next(segment.Path.Length);
        Vector3 position = segment.Path[randomPathPoint];

        Car car = new Car()
        {
            ConnectedSegment = segment.EntityID,
            GroupID = 0,
            Position = position,
            Color = Color.AliceBlue,
            Rotation = Matrix.Identity
        };

        // DrawableAsset asset = new DrawableAsset()
        // {
        //     Position = Matrix.CreateTranslation(position),
        //     Rotation = Matrix.Identity,
        //     Scale = 1
        // };
        
        EntityManager.AddComponentToEntity<Car>(newCarEntity, car);
    }
}