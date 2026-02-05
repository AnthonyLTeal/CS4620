using System;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PlanetaryExpansion;

namespace CS4620IS;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private ArcBallCamera _camera;
    private Terrain _terrain;
    private RoadMesh _roadMesh;
    private CameraControls _cameraControls;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }
    
    // public void OnResize(object sender, EventArgs e)
    // {
    //
    //     float tempZoom = camera.Zoom;
    //     float tempPitch = camera.Pitch;
    //     float tempYaw = camera.Yaw;
    //     camera.AspectRatio = GraphicsDevice.Viewport.AspectRatio;
    //         
    //     camera.Zoom = tempZoom;
    //     camera.Pitch = tempPitch;
    //     camera.Yaw = tempYaw;
    //         
    //     MyraUI.OnScreenSizeChange();
    // }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        _graphics.PreferredBackBufferHeight = (int)(1080);
        _graphics.PreferredBackBufferWidth = (int)(1920);
        _graphics.GraphicsProfile = GraphicsProfile.HiDef;
        _graphics.ApplyChanges();
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        ComponentManager.RegisterComponent<CubeMeshBatcher>();
        ComponentManager.RegisterComponent<ArcBallCamera>();
        // ComponentManager.RegisterComponent<TerrainCursor>();
        // ComponentManager.RegisterComponent<Desktop>();
        ComponentManager.RegisterComponent<Terrain>();
        ComponentManager.RegisterComponent<GraphicsDevice>();
        // ComponentManager.RegisterComponent<ContentManager>();
        ComponentManager.RegisterComponent<RoadMesh>();
        ComponentManager.LoadComponentsFromNamespace("CS4620IS.Components");
        //Assets.Load(Content);
        
        Cursor cursor = new Cursor();
        _terrain = new Terrain(_graphics.GraphicsDevice);
        _camera = new ArcBallCamera(GraphicsDevice.Viewport.AspectRatio, MathHelper.PiOver4, new Vector3(0, 0, 0), Vector3.Up, 0.1f, 1000);
        _cameraControls = new CameraControls();
        CubeMeshBatcher cubeMeshBatcher = new CubeMeshBatcher();
        
        //Assets.Effects["BasicEffect"] = new BasicEffect(GraphicsDevice);
        
        //terrain cursor needed?
        EntityManager.AddComponentToGlobalEntity<Cursor>(cursor);
        EntityManager.AddComponentToGlobalEntity<ArcBallCamera>(_camera);
        EntityManager.AddComponentToGlobalEntity<Terrain>(_terrain);
        EntityManager.AddComponentToGlobalEntity<GraphicsDevice>(GraphicsDevice);
        EntityManager.AddComponentToGlobalEntity<CubeMeshBatcher>(cubeMeshBatcher);
        
        _roadMesh = new RoadMesh(_graphics.GraphicsDevice, this);
        EntityManager.AddComponentToGlobalEntity<RoadMesh>(_roadMesh);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        
        _cameraControls.Update(gameTime, Keyboard.GetState(), Mouse.GetState(), _camera);
        CursorSystem.Update(gameTime);
        _roadMesh.Update(_graphics.GraphicsDevice, _terrain, _camera, Keyboard.GetState());

        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _terrain.Draw(_graphics.GraphicsDevice, _camera);
        _roadMesh.Draw(_graphics.GraphicsDevice, _camera.ViewMatrix, _camera.ProjectionMatrix);
        
        // TODO: Add your drawing code here

        base.Draw(gameTime);
    }
}