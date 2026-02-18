using System;
using MessagePack;

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
    public int? EndConnector = null;
    public int? FrontConnector = null;
    public int ID;
    public int EntityID;
    public int[] AdjacentPathSegmentEntities = new int[2];
    public PathDirection PathDirection; 
    public bool IsLaneRuler = true;
    public int Weight = 1;
    public int Speed = 1;

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