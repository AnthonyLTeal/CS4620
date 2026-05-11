using System;
using System.IO;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CS4620IS;

public class InstancedCarDraw
{
    private static Effect _instancedEffect;
    private static IndexBuffer _unitCubeIndices;
    private static VertexBuffer _unitCubeBuffer;
    private static DynamicVertexBuffer _instanceBuffer;
    private static int _activeCarCount;
    private static int _maximum = 10000;

    public static void Load(GraphicsDevice graphicsDevice, ContentManager content)
    {
        string effectPath = "Shaders/InstancedPrimitive";
        Console.WriteLine("Loading " + effectPath);
        _instancedEffect = content.Load<Effect>(effectPath);
        
        _instanceBuffer = new DynamicVertexBuffer(
            graphicsDevice,
            typeof(InstancedData),
            _maximum,
            BufferUsage.WriteOnly);
        
        VertexPositionColor[] unitCubeVertices = new VertexPositionColor[]
        {
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, -0.5f), Color.White),
            new VertexPositionColor(new Vector3(0.5f, -0.5f, -0.5f), Color.White),
            new VertexPositionColor(new Vector3(0.5f, 0.5f, -0.5f), Color.White),
            new VertexPositionColor(new Vector3(-0.5f, 0.5f, -0.5f), Color.White),
            new VertexPositionColor(new Vector3(-0.5f, -0.5f, 0.5f), Color.White),
            new VertexPositionColor(new Vector3(0.5f, -0.5f, 0.5f), Color.White),
            new VertexPositionColor(new Vector3(0.5f, 0.5f, 0.5f), Color.White),
            new VertexPositionColor(new Vector3(-0.5f, 0.5f, 0.5f), Color.White)
        };

        _unitCubeBuffer = new VertexBuffer(
            graphicsDevice,
            typeof(VertexPositionColor),
            unitCubeVertices.Length,
            BufferUsage.WriteOnly
        );
        
        _unitCubeBuffer.SetData<VertexPositionColor>(unitCubeVertices);
        
        short[] UnitCubeIndices = new short[]
        {
            0,1,2, 0,2,3,   // back
            4,6,5, 4,7,6,   // front
            0,4,5, 0,5,1,   // bottom
            3,2,6, 3,6,7,   // top
            0,3,7, 0,7,4,   // left
            1,5,6, 1,6,2    // right
        };

        _unitCubeIndices = new IndexBuffer(
            graphicsDevice,
            IndexElementSize.SixteenBits,
            UnitCubeIndices.Length,
            BufferUsage.WriteOnly);
        
        _unitCubeIndices.SetData<short>(UnitCubeIndices);
    }

    public static void Update()
    {
        InstancedData[] instancedData = ComponentManager.GetComponents<InstancedData>();
        _activeCarCount = ComponentManager.GetCount<InstancedData>();
        
        if (_activeCarCount == 0)
            return;
        
        _instanceBuffer.SetData(instancedData, 0, _activeCarCount, SetDataOptions.Discard);
    }
    
    public static void Draw(GraphicsDevice graphicsDevice, ArcBallCamera camera)
    {
        if (_activeCarCount == 0)
            return;
        // Inside your Draw method
        graphicsDevice.SetVertexBuffers(
            new VertexBufferBinding(_unitCubeBuffer),           // Stream 0: Geometry
            new VertexBufferBinding(_instanceBuffer, 0, 1)      // Stream 1: Instance Data (Frequency = 1)
        );
        graphicsDevice.Indices = _unitCubeIndices;

        _instancedEffect.Parameters["View"].SetValue(camera.ViewMatrix);
        _instancedEffect.Parameters["Projection"].SetValue(camera.ProjectionMatrix);

        foreach (var pass in _instancedEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawInstancedPrimitives(
                PrimitiveType.TriangleList,
                0,              // Base Vertex
                0,              // Min Vertex Index
                8,              // Num Vertices in the unit cube
                0,              // Start Index
                12,             // Primitive Count per instance (12 triangles)
                _activeCarCount  // Number of Instances to draw
            );
        }
    }
}