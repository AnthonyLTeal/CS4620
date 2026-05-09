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
    private static Texture2D stopLight;
    private static float referenceDistance = 20 ;

    public static void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        stopSign = ImageLoader.LoadImage(graphicsDevice, "StopSignIcon.png", content);
        stopLight = ImageLoader.LoadImage(graphicsDevice, "WhiteCircle.png", content);
    }

    public static void DrawLight(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, int segmentId, PathSegmentConnector connector, int greenIndex)
    {
        ArcBallCamera camera = ComponentManager.GetGlobalComponent<ArcBallCamera>();
        Vector2 origin = new Vector2(stopSign.Width / 2, stopSign.Height / 2);
        float scale = 0.25f;
        
        PathSegment segment = ComponentManager.GetComponent<PathSegment>(segmentId);

        int index = 0;
        if (connector.ID == segment.EndConnector)
            index = segment.Path.Length - 1;
        
        Vector3 position = segment.Path[index];
        
        Vector3 screenPos = graphicsDevice.Viewport.Project(position, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

        if (screenPos.X < 0 || screenPos.Y < 0 || screenPos.X > graphicsDevice.Viewport.Width ||
            screenPos.Y > graphicsDevice.Viewport.Height)
            return;
        
        float distance = Vector3.Distance(position, camera.Position);
        float finalScale = scale * referenceDistance / distance;

        Color color = Color.Red;
        if (greenIndex == connector.CurrentLightGreen)
            color = Color.Yellow;
        
        if (connector.LightTime - connector.LightTimer > connector.YellowTimer && greenIndex == connector.CurrentLightGreen)
            color = Color.Green;
        
        Vector2 pos = new Vector2(screenPos.X, screenPos.Y);
        
        spriteBatch.Draw(stopLight, pos, null, color, 0, origin, finalScale, SpriteEffects.None, 0f);
    }
    
    public static void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
    {
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        spriteBatch.Begin();

        foreach (PathSegmentConnector connector in roadMesh.PathSegmentConnectors)
        {
            for (int i = 0; i < connector.StoplightConnections.Count; i++)
            {
                (int, int) lightConnection = connector.StoplightConnections[i];

                (int segmentOne, int segmentTwo) = lightConnection;
                if (segmentOne != -1)
                {
                    DrawLight(spriteBatch, graphicsDevice, segmentOne, connector, i);
                }
                
                if (segmentTwo != -1)
                {
                    DrawLight(spriteBatch, graphicsDevice, segmentTwo, connector, i);
                }
            }
        }
        
        spriteBatch.End();
    }
}