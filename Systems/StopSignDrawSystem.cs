using System;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using PlanetaryExpansion;

namespace CS4620IS;
//TODO Stop Light Animations still needed, Stop Sign/Stop Light being differentiated still needed, Z scaling still needed

public class StopSignDrawSystem
{
    private static Texture2D stopSign;

    public static void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        stopSign = ImageLoader.LoadImage(graphicsDevice, "StopSignIcon.png", content);
    }
    
    public static void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        ArcBallCamera camera = EntityManager.GetGlobalComponent<ArcBallCamera>();
        Vector2 origin = new Vector2(stopSign.Width / 2, stopSign.Height / 2);
        float scale = 0.25f;
        
        spriteBatch.Begin();

        foreach (PathSegmentConnector connector in roadMesh.PathSegmentConnectors)
        {
            foreach (Vector3 position in connector.StopSignLocations)
            {
                Vector3 screenPos = graphicsDevice.Viewport.Project(position, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

                if (screenPos.X < 0 || screenPos.Y < 0 || screenPos.X > graphicsDevice.Viewport.Width ||
                    screenPos.Y > graphicsDevice.Viewport.Height)
                    continue;
                
                Vector2 pos = new Vector2(screenPos.X, screenPos.Y);
                
                spriteBatch.Draw(stopSign, pos, null, Color.White, 0, origin, scale, SpriteEffects.None, 0f);
            }
            //Vector2 texturePos = new Vector2(screenPos.X, screenPos.Y)
        }
        
        spriteBatch.End();
    }
}