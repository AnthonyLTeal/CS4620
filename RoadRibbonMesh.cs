using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CS4620IS;

public class RoadRibbonMesh
{
    private const int MAX_VERTICES = 50;
    private Color DEFAULT_COLOR = new Color(0.35f, 0.35f, 0.35f);

    private bool _dirty = false;
    private bool _verticesDirty = false;
    private List<Vector3[]> _segmentVertices = new List<Vector3[]>();
    private List<int[]> _segmentIndices = new List<int[]>();
    private int _totalVertexCount;
    private VertexPositionColor[] _vertices;

    //private VertexPositionColor[] _vertices;
    //private int[] _indices;
    private int _triangleCount = 0;
    
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private BasicEffect _basicEffect;
    
    //TODO at some point it might be helpful to create a 1d texture (2d texture with height of 1) and . . .
    //use it as a mapping for nodes or even segments (maybe 1 for each), store the color there or congestion rate
    //then store the index to each node a vertex is connected to and the segment it belongs too and pass that
    //to the GPU to calculate the color of the road when viewing the live congestion

    public RoadRibbonMesh()
    {
        GraphicsDevice graphicsDevice = EntityManager.GetGlobalComponent<GraphicsDevice>();
        _basicEffect = new BasicEffect(graphicsDevice);
    }

    public void UpdateVertexColor(int index, Color color)
    {
        //Console.WriteLine($"Index Updated: {index}");
        _vertices[index].Color = color;
        _verticesDirty = true;
        
        //TODO look into this
        //can also patch the current by setting the data only partially like below rather than setting _verticesDirty and then setting the data for the entire array
        //_roadMesh.RibbonMesh.VertexBuffer.SetData<VertexPositionColor>(
        //    _vertices,      // The source array
        //    startVertex,    // The index in the array to start copying from
        //    elementCount    // The number of vertices to copy
        //);
    }

    public Vector3[] GetSegmentVertices(int ribbonIndex)
    {
        return _segmentVertices[ribbonIndex];
    }

    public int GetVertexCount()
    {
        return _totalVertexCount;
    }

    public int GetSegmentCount()
    {
        return _segmentIndices.Count;
    }

    public void SetVertices()
    {
        _vertexBuffer.SetData(_vertices);
        _verticesDirty = false;
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
        _totalVertexCount += vertices.Length;
    }

    public void Update()
    {
        if (_verticesDirty)
            SetVertices();
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
        
        _vertexBuffer?.Dispose();
        _vertexBuffer = new VertexBuffer(
            graphicsDevice,
            typeof(VertexPositionColor),
            vertices.Count,
            BufferUsage.WriteOnly);
        
        _vertexBuffer.SetData(vertices.ToArray());
        
        _indexBuffer?.Dispose();
        _indexBuffer = new IndexBuffer(
            graphicsDevice,
            IndexElementSize.ThirtyTwoBits,
            indices.Count,
            BufferUsage.WriteOnly);
        
        _indexBuffer.SetData(indices.ToArray());

        _triangleCount = indices.Count / 3;

        _dirty = false;

        _vertices = vertices.ToArray();
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