using System;
using MessagePack;
using PlanetaryExpansion;

namespace CS4620IS.Components;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
public enum PathDirection
{
    Right = 1,
    Left = -1   
}

//ALL SEGMENTS SHOULD ALWAYS HAVE A FRONT AND END CONNECTOR

/// <summary>
/// Field AdjacentPathSegmentEntites = {0,0} by default. 0th Index of entities is global entities so we can use 0 as a condition to see if the adjacent segment exists.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public class PathSegment
{
    //public List<int> connectedIndex = new List<int>();
    private Vector3[] _path;

    [IgnoreMember]
    public Vector3[] Perpendiculars
    {
        get;
        private set;
    }
    
    [IgnoreMember]
    public VertexPositionColor[] DebugPoints
    {
        get;
        set;
    }
    public int? endConnector = null;
    public int? frontConnector = null;
    public int ID;
    public int entityID;
    public int[] AdjacentPathSegmentEntities = new int[2];
    public PathDirection PathDirection; 
    public bool IsLaneRuler = true;
    public int Weight = 1;
    public int Speed = 1;
    public HashSet<int> EntitiesOnSegment = new HashSet<int>();
    public int SlicedParent;
    
    [IgnoreMember]
    public List<BoundingOrientedBox> HitBoxes = new List<BoundingOrientedBox>();

    public int? EndConnector
    {
        get => endConnector;
        set
        {
            endConnector = value;
            StoplightSystems.GenerateStoplights((int)value);
        }
    }
    
    public int? FrontConnector
    {
        get => frontConnector;
        set
        {
            frontConnector = value;
            StoplightSystems.GenerateStoplights((int)value);
        }
    }

    public int EntityID
    {
        get =>  entityID;
        set
        {
            if (value == 0)
            {
                Console.WriteLine("WTF X2: " + value);
            }
            else
            {
                entityID = value;
            }
        }
    }
    
    public int TotalPathLength
    {
        get { return _path.Length + 2; }
    }

    public Vector3[] Path
    {
        get => _path;
        set
        {
            _path = value;
            DebugPoints = PathSegmentSystems.GenerateRoadOutline(value, IsLaneRuler);
            RoadMesh.CreatePathHitBoxes(this);
            //TODO uncomment the following 2 lines to build perpendiculars when roadmesh is fixed
            //if (IsLaneRuler)
                //Perpendiculars = RoadMesh.GeneratePerpendiculars(_path);
        }
    }
}

[MessagePackObject(keyAsPropertyName: true)]
public class PathSegmentConnector
{
    public int ID;
    private Vector3 _position;
    
    [IgnoreMember]
    public VertexPositionColor DebugPosition;
    public List<VertexPositionColor> DebugPoints = new List<VertexPositionColor>();
    public List<int> PointIDs = new List<int>();
    //public List<int> SegmentIDs = new List<int>();
    public List<int> SegmentEntities = new List<int>();
    public DPath DPath;
    //public int Weight = 1;
    public Queue<int> StopQueue = new Queue<int>();
    // new stuff for stoplights
    public List<(int, int)> StoplightConnections;
    public int CurrentLightGreen = 0;
    public double LightTimer = 0;
    public double YellowTimer = 3000;
    public double LightTime = 10000;
    //public List<int> CarEntities = new List<int>(20);

    public Vector3 Position
    {
        get => _position;
        set
        {
            _position = value;
            DebugPosition = new VertexPositionColor(value, Color.Blue);
        }
    }
}

[MessagePackObject]
public class DPath
{
    [Key(0)]public int[] Distances { get; private set; }
    [Key(1)]public int[] Prevs { get; private set; }

    [SerializationConstructor]
    public DPath(int[] _distances, int[] _prevs)
    {
        Distances = _distances;
        Prevs = _prevs;
    }
}