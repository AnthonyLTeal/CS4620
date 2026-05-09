using System;
using System.Linq;
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
    private int? endConnector = null;
    private int? frontConnector = null;
    public int ID;
    public int entityID;
    public int[] AdjacentPathSegmentEntities = new int[2];
    public PathDirection PathDirection; 
    public bool IsLaneRuler = true;
    public int Weight = 1;
    public int Speed = 1;
    public List<int> EntitiesOnSegment = new List<int>();
    public int SlicedParent;
    public int RibbonLength;
    public int RibbonOffset;
    //public int LastColorIndex = 1;
    public int CurrentColorIndex = 1;
    public float CongestionCost = 0;
    public Queue<float> SquaredTimes = new Queue<float>(Enumerable.Repeat(0f, 10));
    public int MaxCarTimes = 10;
    public float AverageSquaredTimer = 0;
    public float AverageSquaredTimeMax = 2.0f;
    public float EstimatedTravelTime;
    
    [IgnoreMember]
    public List<BoundingOrientedBox> HitBoxes = new List<BoundingOrientedBox>();

    public int? EndConnector
    {
        get => endConnector;
        set
        {
            endConnector = value;
            //StoplightSystems.GenerateStoplights((int)value);
        }
    }
    
    public int? FrontConnector
    {
        get => frontConnector;
        set
        {
            frontConnector = value;
            //StoplightSystems.GenerateStoplights((int)value);
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
            EstimatedTravelTime = PathSegmentSystems.GetEstimatedTimeToTravel(this);
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
    public List<Vector3> StopSignLocations = new List<Vector3>();
    public DPath DPath;
    //public int Weight = 1;
    public List<int> StopQueue = new List<int>();
    // new stuff for stoplights
    [IgnoreMember]
    public List<(int, int)> StoplightConnections;
    public int CurrentLightGreen = 0;
    public double LightTimer = 0;
    public double LightTime = 4000;
    public double YellowTimer = 1000;
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
    
    public static string GetPrevsString(DPath dPath)
    {
        return "Prevs: [" + string.Join(", ", dPath.Prevs) + "]";
    }
}