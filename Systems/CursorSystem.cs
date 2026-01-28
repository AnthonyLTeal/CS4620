using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using CS4620IS.Components;

namespace CS4620IS
{
    public class CursorSystem
    {
        public static void Update(GameTime gameTime)
        {
            Cursor cursor = EntityManager.GetGlobalComponent<Cursor>();
            Terrain terrain = EntityManager.GetGlobalComponent<Terrain>();
            ArcBallCamera camera = EntityManager.GetGlobalComponent<ArcBallCamera>();
            GraphicsDevice graphicsDevice = EntityManager.GetGlobalComponent<GraphicsDevice>();

            if (!cursor.isListening)
            {
                cursor.Location = null;
                return;
            }

            Vector3? collisionPoint;

            cursor.TriangleCollided = terrain.GetCursorMappedPoint(out collisionPoint);
            cursor.Location = collisionPoint;
            
            Console.WriteLine($"Cursor Location: {cursor.Location}");

            if (cursor.OnClick != null)
            {
                int inputStateID = ComponentManager.GetComponentID<InputState>();
                InputState inputState = (InputState)EntityManager.EntityComponents[0][inputStateID];

                if (inputState.MouseState.RightButton == ButtonState.Pressed && inputState.RightMousePressed == false)
                {
                    inputState.RightMousePressed = true;
                }

                if (inputState.MouseState.RightButton == ButtonState.Released && inputState.RightMousePressed == true)
                {
                    Console.WriteLine("OnClick Running");
                    inputState.RightMousePressed = false;
                    cursor.OnClick();
                }
            }
        }
    }
}
