using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CS4620IS;

public class RoadRibbonMesh
{
    private const int MAX_VERTICES = 50;
    private Color DEFAULT_COLOR = new Color(0.35f, 0.35f, 0.35f); //red for testing 

    private bool _dirty = false;
    private List<Vector3[]> _segmentVertices = new List<Vector3[]>();
    private List<int[]> _segmentIndices = new List<int[]>();

    //private VertexPositionColor[] _vertices;
    //private int[] _indices;
    private int _triangleCount = 0;
    
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private BasicEffect _basicEffect;

    public RoadRibbonMesh()
    {
        GraphicsDevice graphicsDevice = EntityManager.GetGlobalComponent<GraphicsDevice>();
        _basicEffect = new BasicEffect(graphicsDevice);
    }

    public void Insert(Vector3[] vertices)
    {
        _segmentVertices.Add(vertices);
        int[] indices = new int[(vertices.Length / 2 - 1) * 6];

        int idx = 0;
        for (int i = 0; i < vertices.Length - 2; i += 2)
        {
            //Console.WriteLine($"VERTEX ONE: {vertices[i]}");
            //Console.WriteLine($"VERTEX TWO: {vertices[i + 1]}");
            int left0 = i;
            int right0 = i + 1;
            int left1 = i + 2;
            int right1 = i + 3;

            indices[idx++] = left1;
            indices[idx++] = left0;
            indices[idx++] = right0;

            indices[idx++] = left1;
            indices[idx++] = right0;
            indices[idx++] = right1;
        }
        _segmentIndices.Add(indices);
        _dirty = true;
    }

    public void Update()
    {
        if (_dirty)
            RebuildMesh();
    }

    public void RebuildMesh()
    {
        GraphicsDevice graphicsDevice = EntityManager.GetGlobalComponent<GraphicsDevice>();
        
        //TODO maybe use these instead, they are more accurate but I don't like this style
        // List<VertexPositionColor> vertices = new List<VertexPositionColor>(_segmentVertices.Sum(s => s.Length));
        // List<int> indices = new List<int>(_segmentIndices.Sum(s => s.Length));
        
        List<VertexPositionColor> vertices = new List<VertexPositionColor>(_segmentVertices.Count * MAX_VERTICES);
        List<int> indices = new List<int>(_segmentVertices.Count * MAX_VERTICES);
        
        int vertexOffset = 0;
        for (int i = 0; i < _segmentVertices.Count; i++)
        {
            //add segment verts
            for (int j = 0; j < _segmentVertices[i].Length; j++)
            {
                vertices.Add(new VertexPositionColor()
                {
                    Color = DEFAULT_COLOR, 
                    Position = _segmentVertices[i][j]
                });
            }
            //add segment indices
            for (int j = 0; j < _segmentIndices[i].Length; j++)
            {
                indices.Add(vertexOffset + _segmentIndices[i][j]);
            }

            vertexOffset = vertices.Count;
        }
        
        _vertexBuffer = new VertexBuffer(
            graphicsDevice,
            typeof(VertexPositionColor),
            vertices.Count,
            BufferUsage.WriteOnly);
        
        _vertexBuffer.SetData(vertices.ToArray());
        
        _indexBuffer = new IndexBuffer(
            graphicsDevice,
            IndexElementSize.ThirtyTwoBits,
            indices.Count,
            BufferUsage.WriteOnly);
        
        _indexBuffer.SetData(indices.ToArray());

        _triangleCount = indices.Count / 3;

        _dirty = false;
    }

    public void Draw()
    {
        GraphicsDevice graphicsDevice = EntityManager.GetGlobalComponent<GraphicsDevice>();
        ArcBallCamera camera = EntityManager.GetGlobalComponent<ArcBallCamera>();
        if (_triangleCount == 0)
        {
            return;
        }
        
        graphicsDevice.SetVertexBuffer(_vertexBuffer);
        graphicsDevice.Indices = _indexBuffer;

        _basicEffect.World = Matrix.Identity;
        _basicEffect.View = camera.ViewMatrix;
        _basicEffect.Projection = camera.ProjectionMatrix;

        _basicEffect.EnableDefaultLighting();
        _basicEffect.TextureEnabled = false;
        _basicEffect.VertexColorEnabled = true;

        _basicEffect.LightingEnabled = false;
        //_basicEffect.DiffuseColor = DEFAULT_COLOR;

        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();

            graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                _triangleCount);
        }
    }
}