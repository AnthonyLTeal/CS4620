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
            Cursor cursor = ComponentManager.GetGlobalComponent<Cursor>();
            Terrain terrain = ComponentManager.GetGlobalComponent<Terrain>();

            if (!cursor.isListening)
            {
                cursor.Location = null;
                return;
            }

            Vector3? collisionPoint;

            cursor.TriangleCollided = terrain.GetCursorMappedPoint(out collisionPoint);
            cursor.Location = collisionPoint;
            
            if (cursor.OnClick != null)
            {
                InputState inputState = ComponentManager.GetGlobalComponent<InputState>();

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
