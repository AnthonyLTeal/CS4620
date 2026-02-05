using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CS4620IS;

/// <summary>
/// CubeMeshBatcher will serve as a batcher for cube meshes
/// We will only ever need one of these, so it will be safe to treat
/// as a singleton and place it in the global component.
/// </summary>

public class CubeMeshBatcher
{
    public VertexPositionColor[] Vertices;
    public int[] Indices;
    public int CubeCount = 0;
    public int Maximum;

    /// <summary>
    /// Maximum number of cubes is set in the constructor
    /// Vertices: 8 vertices per cube
    /// Indices: 12 triangles per cube and 3 vertices per triangle and
    /// </summary>
    public CubeMeshBatcher(int maximum = 2000)
    {
        Maximum = maximum;
        Vertices = new VertexPositionColor[Maximum * 8];
        Indices = new int[Maximum * 12 * 3];
    }

    /// <summary>
    /// We set our CubeCount to 0, no need to flush our actual buffers, they will
    /// never change in size, we just change how much of them we show at a given time
    /// </summary>
    public void ClearBuffers()
    {
        CubeCount = 0;
    }

    public void InsertCube(Vector3 position, Matrix rotation, float scale, Color color)
    {

        if (CubeCount >= Maximum)
        {
            //maybe exit and print an out of memory error
            return;
        }

        Vector3[] vertices = UnitCubeVertices;
        int[] indices = UnitCubeIndices;
        
        Matrix world =
            Matrix.CreateScale(scale) *
            rotation *
            Matrix.CreateTranslation(position);
        
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 localVertex = vertices[i];
            VertexPositionColor vertex = new VertexPositionColor()
            {
                Color = color,
                Position = Vector3.Transform(localVertex, world)
            };

            Vertices[CubeCount * 8 + i] = vertex;
        }

        for (int i = 0; i < indices.Length; i++)
        {
            Indices[CubeCount * 36 + i] = indices[i] + CubeCount * 8;
        }

        CubeCount += 1;
    }
    
    public static readonly Vector3[] UnitCubeVertices = new Vector3[]
    {
        new Vector3(-0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, -0.5f),
        new Vector3(-0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(0.5f, 0.5f, 0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f),
    };

    public static readonly int[] UnitCubeIndices = new int[]
    {
        0,1,2, 0,2,3,   // back
        4,6,5, 4,7,6,   // front
        0,4,5, 0,5,1,   // bottom
        3,2,6, 3,6,7,   // top
        0,3,7, 0,7,4,   // left
        1,5,6, 1,6,2    // right
    };
}