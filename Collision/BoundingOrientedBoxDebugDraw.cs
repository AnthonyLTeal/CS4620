using System;
using System.Collections.Generic;
using CS4620IS.Components;

namespace CS4620IS;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class BoundingOrientedBoxDebugDraw
{
    private static BasicEffect effect;

    public static void DrawEntityOOBs()
    {
        ArcBallCamera camera = (ArcBallCamera)EntityManager.GetGlobalComponent<ArcBallCamera>();
        GraphicsDevice graphicsDevice = (GraphicsDevice)EntityManager.GetGlobalComponent<GraphicsDevice>();

        int componentID = ComponentManager.GetComponentID<HitBox>();
        List<int> entities = ComponentManager.GetComponent<HitBox>();

        foreach (int entity in entities)
        {
            HitBox entityData = (HitBox)EntityManager.EntityComponents[entity][componentID];
            if (entityData == null)
            {
                //TODO Look into this bug where entityData can sometimes be null
                Console.WriteLine("Entity " + entity + " does not have a hitbox. Something went wrong here.");
                continue;
            }
            DrawOBB(graphicsDevice, camera, entityData.BoundingOrientedBox.Center, entityData.BoundingOrientedBox.HalfExtent, entityData.BoundingOrientedBox.Orientation);
        }
    }

    public static void DrawOBB(GraphicsDevice graphicsDevice, ArcBallCamera camera, Vector3 center, Vector3 halfExtents, Quaternion rotation)
    {
        if (effect == null)
        {
            effect = new BasicEffect(graphicsDevice)
            {
                VertexColorEnabled = true
            };
        }

        // Define the 8 corners in local space
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(-halfExtents.X, -halfExtents.Y, -halfExtents.Z);
        corners[1] = new Vector3(-halfExtents.X, -halfExtents.Y,  halfExtents.Z);
        corners[2] = new Vector3(-halfExtents.X,  halfExtents.Y, -halfExtents.Z);
        corners[3] = new Vector3(-halfExtents.X,  halfExtents.Y,  halfExtents.Z);
        corners[4] = new Vector3( halfExtents.X, -halfExtents.Y, -halfExtents.Z);
        corners[5] = new Vector3( halfExtents.X, -halfExtents.Y,  halfExtents.Z);
        corners[6] = new Vector3( halfExtents.X,  halfExtents.Y, -halfExtents.Z);
        corners[7] = new Vector3( halfExtents.X,  halfExtents.Y,  halfExtents.Z);

        // Apply rotation and translation to get world positions
        Matrix rotationMatrix = Matrix.CreateFromQuaternion(rotation);
        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = Vector3.Transform(corners[i], rotationMatrix) + center;
        }

        // Define box edges (pairs of indices)
        int[] indices = new int[]
        {
            0,1, 0,2, 1,3, 2,3,   // left face
            4,5, 4,6, 5,7, 6,7,   // right face
            0,4, 1,5, 2,6, 3,7    // connect left/right
        };

        VertexPositionColor[] vertices = new VertexPositionColor[indices.Length];
        for (int i = 0; i < indices.Length; i++)
        {
            vertices[i] = new VertexPositionColor(corners[indices[i]], Color.Blue);
        }

        // Set effect matrices (use your own camera View/Projection here)
        effect.World = Matrix.Identity;
        effect.View = camera.ViewMatrix;         // Replace with your camera’s view matrix
        effect.Projection = camera.ProjectionMatrix; // Replace with your camera’s projection matrix

        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, indices.Length / 2);
        }
    }
}