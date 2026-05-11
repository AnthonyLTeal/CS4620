using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CS4620IS.Components;

public struct InstancedData : IVertexType
{
    public Vector3 Position;
    public float Rotation;
    public float Scale;
    public Color Color;

    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
    (
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.BlendWeight, 0), // Position
        new VertexElement(12, VertexElementFormat.Single, VertexElementUsage.BlendWeight, 1), // Rotation
        new VertexElement(16, VertexElementFormat.Single, VertexElementUsage.BlendWeight, 2), // Scale
        new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 1)          // Color
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}