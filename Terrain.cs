using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlanetaryExpansion;

namespace CS4620IS;

public class Terrain
{
    public float Scale = 500;
    public Vector3 WorldCenter = new Vector3(0, 0, 0);
    public VertexPositionNormalTexture[] Vertices;
    public short[] Indices;
    private VertexBuffer vertexBuffer;
    private IndexBuffer indexBuffer;
    private BasicEffect basicEffect;

    public Terrain(GraphicsDevice graphicsDevice)
    {
        Vertices = new VertexPositionNormalTexture[]
        {
            new VertexPositionNormalTexture(
                new Vector3(-0.5f * Scale, 0f, -0.5f * Scale),
                Vector3.Up,
                new Vector2(0, 1)),

            new VertexPositionNormalTexture(
                new Vector3(-0.5f * Scale, 0f,  0.5f * Scale),
                Vector3.Up,
                new Vector2(0, 0)),

            new VertexPositionNormalTexture(
                new Vector3( 0.5f * Scale, 0f,  0.5f * Scale),
                Vector3.Up,
                new Vector2(1, 0)),

            new VertexPositionNormalTexture(
                new Vector3( 0.5f * Scale, 0f, -0.5f * Scale),
                Vector3.Up,
                new Vector2(1, 1)),
        };
        
        Indices = new short[]
        {
            0, 2, 1,
            0, 3, 2
        };
        
        vertexBuffer = new VertexBuffer(
            graphicsDevice,
            typeof(VertexPositionNormalTexture),
            Vertices.Length,
            BufferUsage.WriteOnly);
        
        vertexBuffer.SetData(Vertices);
        
        indexBuffer = new IndexBuffer(
            graphicsDevice,
            IndexElementSize.SixteenBits,
            Indices.Length,
            BufferUsage.WriteOnly);
        
        indexBuffer.SetData(Indices);

        basicEffect = new BasicEffect(graphicsDevice);

    }
    
    public static Ray CalculateRay(Vector2 mouseLocation, Matrix view, Matrix projection, Viewport viewport)
    {
        Vector3 nearPoint = viewport.Unproject(new Vector3(mouseLocation.X,
                mouseLocation.Y, 0.0f),
            projection,
            view,
            Matrix.Identity);

        Vector3 farPoint = viewport.Unproject(new Vector3(mouseLocation.X,
                mouseLocation.Y, 1.0f),
            projection,
            view,
            Matrix.Identity);

        Vector3 direction = farPoint - nearPoint;
        direction.Normalize();

        return new Ray(nearPoint, direction);
    }

    public short[] GetCursorMappedPoint(out Vector3? collisionPoint)
    {
        collisionPoint = null;
        short[] triangle = null;

        ArcBallCamera camera = ComponentManager.GetGlobalComponent<ArcBallCamera>();
        GraphicsDevice graphics = ComponentManager.GetGlobalComponent<GraphicsDevice>();
        Vector2 mouseLocation = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
        Ray ray = CalculateRay(mouseLocation, camera.ViewMatrix, camera.ProjectionMatrix, graphics.Viewport);
        
        float? t1Dist = null;
        Vector3 t1p1 = Vertices[Indices[0]].Position;
        Vector3 t1p2 = Vertices[Indices[1]].Position;
        Vector3 t1p3 = Vertices[Indices[2]].Position;
        AOneMath.RayIntersectsTriangle(ref ray, ref t1p1, ref t1p2, ref t1p3, out t1Dist);

        float? t2Dist = null;
        Vector3 t2p1 = Vertices[Indices[3]].Position;
        Vector3 t2p2 = Vertices[Indices[4]].Position;
        Vector3 t2p3 = Vertices[Indices[5]].Position;
        AOneMath.RayIntersectsTriangle(ref ray, ref t2p1, ref t2p2, ref t2p3, out t2Dist);

        if (t1Dist != null)
        {
            collisionPoint = ray.Position + ray.Direction * (float)t1Dist;
            triangle = [Indices[0], Indices[1], Indices[2]];
        }

        if (t2Dist != null)
        {
            collisionPoint = ray.Position + ray.Direction * (float)t2Dist;
            triangle = [Indices[3], Indices[4], Indices[5]];
        }

        return triangle;
    }

    public void Draw(GraphicsDevice graphicsDevice, ArcBallCamera camera)
    {
        graphicsDevice.SetVertexBuffer(vertexBuffer);
        graphicsDevice.Indices = indexBuffer;

        basicEffect.World = Matrix.Identity; // make it bigger
        basicEffect.View = camera.ViewMatrix;
        basicEffect.Projection = camera.ProjectionMatrix;

        basicEffect.EnableDefaultLighting();
        basicEffect.TextureEnabled = false;

        basicEffect.LightingEnabled = false;
        basicEffect.DiffuseColor = new Vector3(.9f, 0.9f, 0.9f);

        foreach (var pass in basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();

            graphicsDevice.DrawIndexedPrimitives(
                PrimitiveType.TriangleList,
                0,
                0,
                2);
        }
    }
}