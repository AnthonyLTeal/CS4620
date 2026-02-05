using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CS4620IS;

public class CubeMesh
{
    public void CreateUnitCube(out Vector3[] vertices, out int[] indices)
    {
        // Vertices: (-0.5, -0.5, -0.5) to (0.5, 0.5, 0.5)
        vertices = new Vector3[]
        {
            // Bottom
            new Vector3(-0.5f, -0.5f, -0.5f),// 0
            new Vector3( 0.5f, -0.5f, -0.5f), // 1
            new Vector3( 0.5f, -0.5f,  0.5f),// 2
            new Vector3(-0.5f, -0.5f,  0.5f),// 3

            // Top
            new Vector3(-0.5f,  0.5f, -0.5f), // 4
            new Vector3( 0.5f,  0.5f, -0.5f),// 5
            new Vector3( 0.5f,  0.5f,  0.5f),// 6
            new Vector3(-0.5f,  0.5f,  0.5f)// 7
        };

        indices = new int[]
        {
            0, 2, 1,
            0, 3, 2,
            4, 5, 6,
            4, 6, 7,
            3, 6, 2,
            3, 7, 6,
            0, 1, 5,
            0, 5, 4,
            0, 4, 7,
            0, 7, 3,
            1, 2, 6,
            1, 6, 5
        };
    }
}