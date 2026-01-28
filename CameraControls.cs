using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace PlanetaryExpansion
{
    class CameraControls
    {
        private float previousScrollValue = 0;
        private float yawAnchor = 0;
        private float pitchAnchor = 0;
        private Vector2 mouseAnchor = new Vector2(0, 0);
        private bool rotating;
        private float rotationSpeed = 0;
        private bool followingRotation = false;
        private float minCamTiltZoom = 200;
        private float maxCamTiltZoom = 300;
        private float minCamDistance = 10;
        private Vector3 maxLookUp = Vector3.Up * 30;

        public void SetFollowRotation(float _rotationSpeed)
        {
            followingRotation = true;
            rotationSpeed = _rotationSpeed;
        }

        public void TiltCamera(ArcBallCamera camera)
        {
            if (camera.Zoom < maxCamTiltZoom)
            {
                float t = (maxCamTiltZoom - camera.Zoom) / (maxCamTiltZoom - minCamTiltZoom);
                Vector3 tiltOffset = Vector3.Lerp(Vector3.Zero, maxLookUp, t);
                camera.LookAt = Vector3.Zero + tiltOffset;
            }
        }

        public void Update(GameTime gameTime, KeyboardState keyState, MouseState mouse, ArcBallCamera camera)
        {
            //Zoom out
            if (mouse.ScrollWheelValue < previousScrollValue)
            {
                camera.Zoom += (float)gameTime.ElapsedGameTime.TotalMilliseconds * .5f;
                //TiltCamera(camera);
                // if (camera.Zoom > 200 && camera.Zoom < 300)
                // {
                //     camera.LookAt -= Vector3.Up * 3;
                //     camera.Pitch -= 0.015f;
                // }
            }

            //Zoom in
            if (mouse.ScrollWheelValue > previousScrollValue)
            {
                if (camera.Zoom > minCamDistance)
                {
                    camera.Zoom -= (float)gameTime.ElapsedGameTime.TotalMilliseconds * .5f;
                    //TiltCamera(camera);
                }
            }
            
            previousScrollValue = mouse.ScrollWheelValue;

            if (mouse.MiddleButton == ButtonState.Pressed)
            {
                if (!rotating)
                {
                    rotating = !rotating;
                    yawAnchor = camera.Yaw;
                    pitchAnchor = camera.Pitch;
                    mouseAnchor.X = mouse.Position.X;
                    mouseAnchor.Y = mouse.Position.Y;
                }

                if (rotating)
                {
                    camera.Yaw = yawAnchor - (mouse.Position.X - mouseAnchor.X) * .01f;
                    camera.Pitch = pitchAnchor - (mouse.Position.Y - mouseAnchor.Y) * .01f;
                }
            }

            if (mouse.MiddleButton == ButtonState.Released && rotating)
            {
                rotating = !rotating;
            }

            if (followingRotation) {
                camera.Yaw += rotationSpeed;
            }

            // if (keyState.IsKeyDown(Keys.Q))
            // {
            //     camera.Yaw += (float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f;
            //     //camera.MoveCameraRight(0);
            // }
            //
            // if (keyState.IsKeyDown(Keys.E))
            // {
            //     camera.Yaw -= (float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.001f;
            //     //camera.MoveCameraRight(0);
            // }
            //
            if (keyState.IsKeyDown(Keys.D))
            {
                camera.MoveCameraRight(0.01f * (float)gameTime.ElapsedGameTime.TotalMilliseconds);
                //camera.Zoom -= (float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.2f;
            }
            
            if (keyState.IsKeyDown(Keys.A))
            {
                camera.MoveCameraRight(-0.01f * (float)gameTime.ElapsedGameTime.TotalMilliseconds);
                //camera.Zoom += (float)gameTime.ElapsedGameTime.TotalMilliseconds * 0.2f;
            }
            
            if (keyState.IsKeyDown(Keys.W))
            {
                camera.MoveCameraForward(0.01f * (float)gameTime.ElapsedGameTime.TotalMilliseconds);
            }
            
            if (keyState.IsKeyDown(Keys.S))
            {
                camera.MoveCameraForward(-0.01f * (float)gameTime.ElapsedGameTime.TotalMilliseconds);
            }
        }
    }
}
