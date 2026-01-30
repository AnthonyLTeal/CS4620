using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CS4620IS;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using CS4620IS.Components;

namespace PlanetaryExpansion;
public struct VertexUVTextured
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 TextureCoordinate;

    public static int SizeInBytes = (3 + 3 + 2) * sizeof(float);
    public static VertexElement[] VertexElements = new VertexElement[]
    {
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0 ),
        new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0 ),
        new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0 )
    };
}

public enum RoadAngle
{
    East,
    West,
    North,
    South
}

public enum State
{
    One,
    Two,
    Three
}

public class RoadMesh
{
    public List<PathSegment> Segments = new List<PathSegment>();
    public int MaxSegmentLength = 25;
    public float Spacing = 0.75f;
    public float Resolution = 50;
    public Vector3? StartingConnectingCursorPoint = null;
    public Vector3 ClampedPointForAngle;
    public PathSegment ClampedSegment;
    public PathSegment OutlineSegment;
    public Game1 game;
    private RoadAngle roadAngle;
    private PathSegment P1ClampedSegment;
    private Vector3 p3ClampedPoint;
    private PathSegmentConnector clampedConnector;
    // private RoadAngle clampedRoadAngle;
    private int P1ClampedSegmentPathIndex;
    private State state = State.One;
    private int? StateOneSnappedSegmentEntity = null;
    private int? StateOneSnappedPoint = null;
    private int? StateThreeSnappedSegmentEntity = null;
    private int? StateThreeSnappedPoint = null;
    private int? StateOneSnappedSegmentConnector = null;
    private int? StateThreeSnappedSegmentConnector = null;
    public OctreeSuper<PathSegmentPointOctreeData> Octree;
    private PathSegmentPointOctreeData ClampedPointOctreeData = null;
    public bool CreateEnabled = true;
    private const float PATH_WIDTH = 1;
    private RoadRibbonMesh _ribbonMesh = new RoadRibbonMesh();
    
    public List<PathSegmentConnector> PathSegmentConnectors = new List<PathSegmentConnector>();

    public RoadMesh(GraphicsDevice graphicsDevice, Game1 _game)
    {
        basicEffect = new BasicEffect(graphicsDevice);
        game = _game;

        Terrain terrain = (Terrain)EntityManager.GetGlobalComponent<Terrain>();        
        Octree = new OctreeSuper<PathSegmentPointOctreeData>(terrain.Scale * 2, 4, terrain.WorldCenter);
        _ribbonMesh = new RoadRibbonMesh();
        //Octree = new OctreeSuper<PathSegmentOctreeData>(100, 4, terrain.WorldCenter);
        // PathSegmentOctreeData data = new PathSegmentOctreeData();
        // data.Position = new Vector3(2, 30, 2);
        // data.PointIndex = 100;
        // data.EntityID = 10;
        // Octree.InsertElementToOctreeFromPoint(new Vector3(2,30f,2), data);
        //
        // List<OctreeElement<PathSegmentOctreeData>> elements = Octree.GetElementListsFromSphere(new Vector3(-1, 30, 2), 5);
        // Console.WriteLine("Total Elements Found: " + elements.Count);

        // OctreeElement<PathSegmentOctreeData> element = Octree.GetElementListFromLocation(new Vector3(2, 30, 2));
        // Console.WriteLine("Data Position: " + element.Data.Position);
        // Console.WriteLine("Data Point Index: " + element.Data.PointIndex);
        // Console.WriteLine("Next Node: " + element.NextElement);
    }

    private bool meshGenerated = false;

    //TODO the portion using the ClampVerticeToTerrain function should be optimized in some way since it's not the quickest to update every frame
    //could only update once every so often to reduce times per frame it's being updated or something I don't know
    //could potentially use a shader to cast this to the object with the depth texture
    public PathSegment GenerateOutlineMesh(GraphicsDevice graphicsDevice, Vector3 p0, Vector3 p1, Vector3 controlPoint, Terrain terrain, ArcBallCamera camera)
    {
        if (meshGenerated == false)
        {
            meshGenerated = true;
        }
        (Vector3 cp1, Vector3 cp2) = CalculateControlPoints(p0, p1, controlPoint);
        Vector3[] segmentPoints = CalculateEvenlySpacedPoints(Spacing, p0, p1, cp1, cp2, 100);
        ClampVerticesToTerrain(segmentPoints, terrain, camera, graphicsDevice);
        //int[] indices = new int[0];

        return new PathSegment()
        {
            Path = segmentPoints
        };
    }
    
    public List<PathSegment> GeneratePath(GraphicsDevice graphicsDevice, Vector3 p0, Vector3 p1, Vector3 controlPoint, Terrain terrain, ArcBallCamera camera)
    {
        int componentID = ComponentManager.GetComponentID<PathSegment>();
        PathSegment stateOneSegment = StateOneSnappedSegmentEntity == null ? null : (PathSegment)EntityManager.EntityComponents[(int)StateOneSnappedSegmentEntity][componentID];
        PathSegmentConnector stateOneNewConnector = null;
        PathSegmentConnector stateThreeNewConnector = null;
        //slice path for state one snapped point (beginning point for new road segments)
        bool sliceStateOne = (StateOneSnappedSegmentEntity != null && StateOneSnappedPoint != 0 && StateOneSnappedPoint != stateOneSegment.Path.Length - 1);
        if (sliceStateOne)
        {
            SlicePathSegment((int)StateOneSnappedSegmentEntity, (int)StateOneSnappedPoint);
            StateOneSnappedSegmentConnector = PathSegmentConnectors.Count - 1;
            
            //remap the points for state three if snapped and greater than stateone
            //this does both since if StateThreeSnappedPoint is null, this will always be false
            //TODO need to review this, someting isn't right with the indices
            if (StateThreeSnappedPoint > StateOneSnappedPoint && StateThreeSnappedSegmentEntity == StateOneSnappedSegmentEntity)
            {
                StateThreeSnappedPoint = StateThreeSnappedPoint - StateOneSnappedPoint - 1;
                PathSegmentConnector connector = PathSegmentConnectors[(int)stateOneSegment.EndConnector];
                StateThreeSnappedSegmentEntity = connector.SegmentEntities[0] == StateOneSnappedSegmentEntity ? connector.SegmentEntities[1] : connector.SegmentEntities[0];
            }
        }
        
        //slice path for state three snapped point (end point for new road segments)
        if (StateThreeSnappedSegmentEntity != null && StateThreeSnappedPoint != 0 && StateThreeSnappedPoint != ((PathSegment)EntityManager.EntityComponents[(int)StateThreeSnappedSegmentEntity][componentID]).Path.Length - 1)
        {
            SlicePathSegment((int)StateThreeSnappedSegmentEntity, (int)StateThreeSnappedPoint);
            StateThreeSnappedSegmentConnector = PathSegmentConnectors.Count - 1;
        }

        meshGenerated = false;
        List<PathSegment> segments = new List<PathSegment>();

        (Vector3 cp1, Vector3 cp2) = CalculateControlPoints(p0, p1, controlPoint);
        Vector3[] bezierPoints = CalculateEvenlySpacedPoints(Spacing, p0, p1, cp1, cp2, Resolution);
        ClampVerticesToTerrain(bezierPoints, terrain, camera, graphicsDevice);

        //if only one segment, it must have at least 4 points, otherwise the segment will be converted to
        //2 segment connectors and cause errors when generating. Segment connectors must be connected to 
        //a road segment
        if (bezierPoints.Length < 4)
        {
            Console.WriteLine("Not Long Enough");
            return null;
        }

        int totalSegments = bezierPoints.Length / MaxSegmentLength + Math.Min(bezierPoints.Length % MaxSegmentLength, 1);
        //Console.WriteLine("Total Segments: " + totalSegments);

        //PathSegment previousSegment = null;
        int pointCounter = 0;

        for (int i = 0; i < totalSegments; i ++)
        {
            int segmentLength = MaxSegmentLength;
            if (i == totalSegments -1 && bezierPoints.Length % MaxSegmentLength > 0)
            {
                segmentLength = bezierPoints.Length % MaxSegmentLength;
            }

            if (i == totalSegments - 2 && bezierPoints.Length % MaxSegmentLength < 4)
            {
                segmentLength += bezierPoints.Length % MaxSegmentLength;
                totalSegments -= 1;
            }

            if (i !=0 )
            {
                segmentLength += 1;
            }

            Vector3[] segmentPoints = new Vector3[segmentLength];
            for (int j = 0; j < segmentLength; j ++)
            {
                segmentPoints[j] = bezierPoints[pointCounter];
                if (j != segmentLength - 1)
                {
                    pointCounter += 1;
                }
            }

            //TODO need to refactor this function to decouple it from the class and use ECS
            List<int> segmentEntities = ComponentManager.GetComponent<PathSegment>();

            PathSegment newSegment = new PathSegment()
            {
                Path = segmentPoints,
                ID = segmentEntities.Count
            };

            // if (i == totalSegments - 1)
            // {
            //     PathSegmentConnector pathSegmentConnector = new PathSegmentConnector();
            //     PathSegmentConnectors.Add(pathSegmentConnector);
            //     pathSegmentConnector.ID = PathSegmentConnectors.Count - 1;
            //     pathSegmentConnector.SegmentIDs.Add(newSegment.ID);
            // }

            // if (StateOneClampedSegment != null)
            // {
            //     newSegment.AddConnectedSegment(Segments[(int)StateOneClampedSegment], 0, (int)StateOneClampedPoint);
            //     Segments[(int)StateOneClampedSegment].AddConnectedSegment(newSegment, (int)StateOneClampedPoint, 0);
            // }

            segments.Add(newSegment);
            EntityManager.AddEntity();
            int segmentEntity = EntityManager.LastAddedEntity;
            EntityManager.AddComponentToEntity<PathSegment>(segmentEntity, newSegment);
            Segments.Add(newSegment);
            newSegment.EntityID = segmentEntity;
        }

        //start here
        //this section causes a miscount of the number of points in each segment with the first and last segments
        //this causes a bug the path connectors knowing which segment/point they are connected to and make it irremovable
        //can probably add beginning and ending connectors in the GeneratePathSegmentConnectors function to make this a little easier to keep track of
        GeneratePathSegmentConnectors(segments);
        
        if (StateOneSnappedSegmentEntity == null && StateOneSnappedSegmentConnector == null)
        {
            AddBeginConnectorToNewPath(segments[0]);
        }
        
        if (StateThreeSnappedSegmentEntity == null && StateThreeSnappedSegmentConnector == null)
        {
            AddEndConnectorToNewPath(segments[^1]);
        }
        //end here

        if (StateOneSnappedSegmentConnector != null)
        {
            Console.WriteLine("State One Snapped Connector: " + StateOneSnappedSegmentConnector);

            PathSegment segment = segments[0]; 
            Vector3[] newPath = new Vector3[segment.Path.Length - 1];

            for (int i = 0; i < segment.Path.Length - 1; i++)
            {
                newPath[i] = segment.Path[i + 1];
            }
            segment.Path = newPath;

            PathSegmentConnectorSystems.AddSegmentToSegmentConnector(PathSegmentConnectors[(int)StateOneSnappedSegmentConnector], segment, SegmentConnectorIndex.First);
        }
        
        if (StateThreeSnappedSegmentConnector != null)
        {
            Console.WriteLine("State One Snapped Connector: " + StateThreeSnappedSegmentConnector);
            Vector3[] path = new Vector3[segments[^1].Path.Length - 1];
            for (int i = 0; i < segments[^1].Path.Length - 1; i++)
            {
                path[i] = segments[^1].Path[i];
            }
        
            segments[^1].Path = path;
            PathSegmentConnectorSystems.AddSegmentToSegmentConnector(PathSegmentConnectors[(int)StateThreeSnappedSegmentConnector], segments[^1], SegmentConnectorIndex.Last);
        }

        return segments;
    }

    public void AddEndConnectorToNewPath(PathSegment segment)
    {
        PathSegmentConnector connector = new PathSegmentConnector();
        connector.Position = segment.Path[^1];
        //EntityManager.AddEntity();
        //EntityManager.AddComponentToEntity<PathSegmentConnector>(EntityManager.LastAddedEntity, connector);
        PathSegmentConnectors.Add(connector);
        connector.ID = PathSegmentConnectors.Count - 1;
        Vector3[] previousSegmentPath = segment.Path;
        Array.Resize(ref previousSegmentPath, previousSegmentPath.Length-1);
        segment.Path = previousSegmentPath;
        segment.EndConnector = connector.ID;
        PathSegmentConnectorSystems.AddSegmentToSegmentConnector(connector, segment, SegmentConnectorIndex.Last);
        InsertSegmentConnectorIntoOctree(connector);
    }

    public void AddBeginConnectorToNewPath(PathSegment segment)
    {
        PathSegmentConnector connector = new PathSegmentConnector();
        connector.Position = segment.Path[0];
        //EntityManager.AddEntity();
        //EntityManager.AddComponentToEntity<PathSegmentConnector>(EntityManager.LastAddedEntity, connector);
        PathSegmentConnectors.Add(connector);
        connector.ID = PathSegmentConnectors.Count - 1;
        Vector3[] newPath = new Vector3[segment.Path.Length - 1];

        for (int i = 0; i < segment.Path.Length - 1; i++)
        {
            newPath[i] = segment.Path[i + 1];
        }
        
        segment.Path = newPath;
        segment.FrontConnector = connector.ID;
        PathSegmentConnectorSystems.AddSegmentToSegmentConnector(connector, segment, SegmentConnectorIndex.First);
        InsertSegmentConnectorIntoOctree(connector);

        if (segment.EndConnector == null) //don't think should be null lol
            return;

        PathSegmentConnector endConnector = PathSegmentConnectors[(int)segment.EndConnector];
        
        //Console.WriteLine($"Checking against Entity: {segment.EntityID}");
        for (int i = 0; i < endConnector.SegmentEntities.Count; i ++)
        {
            //Console.WriteLine($"Entity: {endConnector.SegmentEntities[i]} Path Point: {endConnector.PointIDs[i]}");
            if (endConnector.SegmentEntities[i] == segment.EntityID)
                endConnector.PointIDs[i] -= 1;
        }
    }

    //TODO Review the code that's using this and maybe change the collection to a list instead
    //this works for now because it also keeps the order of the array
    private T[] RemoveElementFromArrayAndResize<T>(int index, T[] array)
    {
        T[] newArray = new T[array.Length - 1];

        bool removed = false;
        for (int i = 0; i < array.Length; i++)
        {
            if (index == i)
            {
                removed = true;
                continue;
            }

            if (removed)
            {
                newArray[i - 1] = array[i];
            }
            else
            {
                newArray[i] = array[i];
            }
        }

        return newArray;
    }

    public void GeneratePathSegmentConnectors(List<PathSegment> segments)
    {
        PathSegment previousSegment = null;
        //Console.WriteLine("PathSegments: " + segments.Count);

        for (int i = 0; i < segments.Count; i++)
        {
            //Console.WriteLine("CurrentSegment: " + i);
            PathSegment newSegment = segments[i];

            if (previousSegment != null)
            {
                PathSegmentConnector pathSegmentConnector = new PathSegmentConnector();
                PathSegmentConnectors.Add(pathSegmentConnector);
                pathSegmentConnector.ID = PathSegmentConnectors.Count - 1;
                pathSegmentConnector.Position = newSegment.Path[0];

                newSegment.Path = RemoveElementFromArrayAndResize(0, newSegment.Path);
                //pathSegmentConnector.Position = previousSegment.Path[^1];
                pathSegmentConnector.PointIDs.Add(0);
                pathSegmentConnector.SegmentEntities.Add(newSegment.EntityID);
                pathSegmentConnector.DebugPoints.Add(new VertexPositionColor(newSegment.Path[0], Color.Blue));
                newSegment.FrontConnector = PathSegmentConnectors.Count - 1;
                //var previousSegmentPath = previousSegment.Path;
                //Array.Resize<Vector3>(ref previousSegmentPath, previousSegment.Path.Length-1);
                previousSegment.Path = RemoveElementFromArrayAndResize(previousSegment.Path.Length - 1, previousSegment.Path);
                pathSegmentConnector.PointIDs.Add(previousSegment.Path.Length - 1);
                pathSegmentConnector.SegmentEntities.Add(previousSegment.EntityID);
                pathSegmentConnector.DebugPoints.Add(new VertexPositionColor(previousSegment.Path[previousSegment.Path.Length - 1], Color.Blue));
                previousSegment.EndConnector = PathSegmentConnectors.Count - 1;
                InsertSegmentConnectorIntoOctree(pathSegmentConnector);
            }

            previousSegment = segments[i];
        }
        
        //TODO can probably use a plane intersection to find where the road intersects more evenly
        //this will require 2 plane intersections per 

        //bug this will not work correctly if there is a double cut on the initial road segment, need to fix
        //probably fixed?
        // if (StateOneSnappedSegment != null)
        // {
        //     PathSegment newSegment = segments[0];
        //     PathSegment clampedSegment = Segments[(int)StateOneSnappedSegment];
        //     if (StateOneSnappedPoint == 0)
        //     {
        //         PathSegmentConnectors[(int)clampedSegment.FrontConnector].SegmentIDs.Add(newSegment.ID);
        //         PathSegmentConnectors[(int)clampedSegment.FrontConnector].PointIDs.Add(0);
        //         newSegment.FrontConnector = clampedSegment.FrontConnector;
        //         Console.WriteLine("Total Segments on Connector: " + PathSegmentConnectors[(int)clampedSegment.FrontConnector].SegmentIDs.Count);
        //     }
        //
        //     if (StateOneSnappedPoint == clampedSegment.Path.Length - 1)
        //     {
        //         PathSegmentConnectors[(int)clampedSegment.EndConnector].SegmentIDs.Add(newSegment.ID);
        //         PathSegmentConnectors[(int)clampedSegment.EndConnector].PointIDs.Add(0);
        //         newSegment.FrontConnector = clampedSegment.EndConnector;
        //         Console.WriteLine("Total Segments on Connector: " + PathSegmentConnectors[(int)clampedSegment.FrontConnector].SegmentIDs.Count);
        //     }
        // }
        // //
        // if (StateThreeSnappedSegment != null)
        // {
        //     PathSegment newSegment = segments[segments.Count - 1];
        //     PathSegment clampedSegment = Segments[(int)StateThreeSnappedSegment];
        //     if (StateThreeSnappedPoint == 0)
        //     {
        //         PathSegmentConnectors[(int)clampedSegment.FrontConnector].SegmentIDs.Add(newSegment.ID);
        //         PathSegmentConnectors[(int)clampedSegment.FrontConnector].PointIDs.Add(newSegment.Path.Length - 1);
        //         newSegment.EndConnector = clampedSegment.FrontConnector;
        //     }
        //
        //     if (StateThreeSnappedPoint == clampedSegment.Path.Length - 1)
        //     {
        //         PathSegmentConnectors[(int)clampedSegment.EndConnector].SegmentIDs.Add(newSegment.ID);
        //         PathSegmentConnectors[(int)clampedSegment.EndConnector].PointIDs.Add(newSegment.Path.Length - 1);
        //         newSegment.EndConnector = clampedSegment.EndConnector;
        //     }
        // }
        //
        // // //add connector to initial point, skip if clamped since it will be added to sliced connector
        // if (StateOneSnappedSegment == null)
        // {
        //     PathSegmentConnector pathSegmentConnector = new PathSegmentConnector();
        //     PathSegmentConnectors.Add(pathSegmentConnector);
        //     pathSegmentConnector.ID = PathSegmentConnectors.Count - 1;
        //     pathSegmentConnector.PointIDs.Add(0);
        //     pathSegmentConnector.SegmentIDs.Add(segments[0].ID);
        //     segments[0].FrontConnector = PathSegmentConnectors.Count - 1;
        // }
        //
        // // //add connector to final road point
        // if (StateThreeSnappedSegment == null)
        // {
        //     int segmentID = segments[segments.Count - 1].ID;
        //     int pathID = segments[segments.Count - 1].Path.Length - 1;
        //     PathSegmentConnector connector = new PathSegmentConnector();
        //     PathSegmentConnectors.Add(connector);
        //     connector.ID = PathSegmentConnectors.Count - 1;
        //     connector.PointIDs.Add(pathID);
        //     connector.SegmentIDs.Add(segmentID);
        //     segments[segments.Count - 1].EndConnector = PathSegmentConnectors.Count - 1;
        // }
    }
    
    /// <param name="segment">Segment directly adjacent to a ruler segment</param>
    /// <param name="previousSegment">If called directly, provide the adjacent ruler segment</param>
    /// <returns> Returns the outermost segment away from the ruler from the segment provided</returns>
    public static PathSegment GetOuterMostSegment(PathSegment segment, PathSegment previousSegment)
    {
        if (segment.AdjacentPathSegmentEntities[0] != 0 && segment.AdjacentPathSegmentEntities[0] != previousSegment.EntityID)
        {
            int componentID = ComponentManager.GetComponentID<PathSegment>();
            int nextSegmentID = segment.AdjacentPathSegmentEntities[0];
            PathSegment nextSegment = (PathSegment)EntityManager.EntityComponents[nextSegmentID][componentID];
            return GetOuterMostSegment(nextSegment, segment);
        }
        if (segment.AdjacentPathSegmentEntities[1] != 0 && segment.AdjacentPathSegmentEntities[1] != previousSegment.EntityID)
        {
            int componentID = ComponentManager.GetComponentID<PathSegment>();
            int nextSegmentID = segment.AdjacentPathSegmentEntities[1];
            PathSegment nextSegment = (PathSegment)EntityManager.EntityComponents[nextSegmentID][componentID];
            return GetOuterMostSegment(nextSegment, segment);
        }

        return segment;
    }

    public (PathSegment, PathSegment) GetOuterMostSegments(PathSegment rulerSegment)
    {
        int componentID = ComponentManager.GetComponentID<PathSegment>();
        int o1EntityID = rulerSegment.AdjacentPathSegmentEntities[0];
        int o2EntityID = rulerSegment.AdjacentPathSegmentEntities[1];
        PathSegment o1Segment = (PathSegment)EntityManager.EntityComponents[o1EntityID][componentID];
        PathSegment o2Segment = (PathSegment)EntityManager.EntityComponents[o2EntityID][componentID];
        o1Segment = GetOuterMostSegment(o1Segment, rulerSegment);
        o2Segment = GetOuterMostSegment(o2Segment, rulerSegment);

        return (o1Segment, o2Segment);
    }
    
    //this will be rectangular intersection
    //midpoint between 2 path points will be the up/down direction
    private float? RayIntersectsPath(Ray ray, Vector3[] path)
    {
        for (int i = 0; i < path.Length - 2; i++)
        {
            Vector3 horizontalVector = path[i + 1] - path[i];
            float distance = Vector3.Distance(path[i + 1], path[i]);
            Vector3 centerPoint = horizontalVector * (distance/2);
            Vector3 verticalVector = centerPoint;
            verticalVector.Normalize();
            Vector3 p1 = path[i] + verticalVector * distance;
            Vector3 p2 = path[i + 1] + verticalVector * distance;
            Vector3 p3 = path[i] - verticalVector * distance;
            Vector3 normal = Vector3.Cross(p2 - p1, p3 - p1);
            Plane plane = new Plane(centerPoint, normal);
            float? squareCollision = ray.Intersects(plane);
            //float? squareCollision = ray.IntersectsRectangle( centerPoint, normal, distance, distance);
            if (squareCollision != null)
                return squareCollision;
        }
        return null;
    }
    
    private bool PathIntersectsSegment(Vector3[] path, PathSegment targetSegment, int dir = 1)
    {   
        float shortestMaxDistance = Vector3.Distance(path[path.Length - 1], path[0]);
        Vector3 rayDir = path[path.Length - 1] - path[0];
        rayDir.Normalize();
        Ray ray = new Ray(path[0], rayDir);
        float? distance = RayIntersectsPath(ray, targetSegment.Path);
        Console.WriteLine("Distance: " + distance);
        if (distance == null)
            return false;
        if ((float)distance < shortestMaxDistance)
        {
            return true;
        }
                
        return false;
    }
    
    public static Vector3 Calculate3DTangentNormal(Vector3 p1, Vector3 p2)
    {
        // Vector3 worldCenter = EntityManager.GetGlobalComponent<Terrain>().WorldCenter;
        Vector3 u = p2 - p1;
        Vector3 v = Vector3.Up;
        Vector3 normal = Vector3.Cross(u, v);
        normal.Normalize();
        return normal;
    }

    private PathSegment GetIntersectedOuterSegment(Vector3[] path, PathSegment targetRulerSegment)
    {
        (PathSegment o1Segment, PathSegment o2Segment) = GetOuterMostSegments(targetRulerSegment);
        if (PathIntersectsSegment(path, o1Segment))
            return o1Segment;
        if (PathIntersectsSegment(path, o2Segment))
            return o2Segment;
        return null;
    }

    /// <returns>float distance, int index: of originSegment clamped (next/previous point should be used as vector direction)</returns>
    private (float?, int) ClampSegmentsAtOrigin(PathSegment originSegment, int originClampedIndex, PathSegment targetSegment)
    {
        // for (int i = 0; i < originSegment.Path.Length - 2; i++)
        // {
        //     Vector3 p1 = originSegment.Path[i];
        //     Vector3 origin = originSegment.Path[i + 1];
        //     Vector3 vector = p1 - origin;
        //     vector.Normalize();
        //     Ray ray = new Ray(origin, vector);
        // }
        return (null, 0);
    }

    //we are going to brute force this for now and see how it runs
    //it may be okay, maybe not
            
    //need to find the closest segment intersection
    //should iterate through all segments and all points on the segment
    //can drop points by checking distance against the origin point. 
    //if it's shorter than the road width * some_constant (this might be actually the max distance between each point on a path)
    //it should be too far to be considered a potential collision
    //otherwise we can always check rectangle collision between the 2 points with some constant height
    //each point with a +1 point should be collided against the ray
            
    //check rays against segments first. When a ray successfully collides with segment (rectangle not just plane) can save that one to see if it has the shortest distance
    //after checking planar and rectangular intersection, break out of ray checking loop and save
    //this collision distance, vector direction and origin point
                
    //could check distance from center point of the path from origin segment to see 
            
    //to find the closest outer segment to collision from the 
    //check ray (origin initial point of segment, dir is from first to last point) collision against each plane of the target
    //if shortest distance is greater than distance between the origin point and initial point, we know the final point is too deep
    //should also drop the final point and go to the one behind it and check again until the closest collision is shorter
    public void GetOuterSegmentRayClamp(PathSegment outerSegment, PathSegment sourceRulerSegment, PathSegment clampedSegment)
    {
        
        PathSegmentConnector pathSegmentConnector;
        
        if (StateOneSnappedSegmentEntity != null)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            if (StateOneSnappedPoint == 0) {
                pathSegmentConnector = PathSegmentConnectors[(int)sourceRulerSegment.FrontConnector];
            } else {
                pathSegmentConnector = PathSegmentConnectors[(int)sourceRulerSegment.EndConnector];
            }
            // } else if (StateOneSnappedPoint == clampedSegment.Path.Length - 1)
            // {
            //     pathSegmentConnector = PathSegmentConnectors[(int)sourceRulerSegment.EndConnector];
            // } else
            // {
            //     pathSegmentConnector = null;
            //     return;
            // }

            float? shortestCollisionDistance = null;
            PathSegment closestSegment = null;
            int pointIndexOfPath;
            
            Console.WriteLine("Segments to check: " + pathSegmentConnector.SegmentEntities.Count);
            
            for (int i = 0; i < pathSegmentConnector.SegmentEntities.Count; i++)
            {
                PathSegment currentRulerSegment = Segments[pathSegmentConnector.SegmentEntities[i]];

                if (currentRulerSegment == sourceRulerSegment)
                    continue;
                
                //if this happens we know it's the correct segment to clamp the distance too
                //otherwise we need to continue replacing and checking for the shortest collision distance and closest segment and point index for the ray
                PathSegment intersectedSegment = GetIntersectedOuterSegment(outerSegment.Path, currentRulerSegment);
                if (intersectedSegment != null)
                {
                    (shortestCollisionDistance, pointIndexOfPath) = ClampSegmentsAtOrigin(outerSegment, 0, intersectedSegment);
                    closestSegment = intersectedSegment;
                    break;
                }

                (PathSegment s1, PathSegment s2) = GetOuterMostSegments(currentRulerSegment);
                (float? s1Distance, int s1Index) = ClampSegmentsAtOrigin(outerSegment, 0, s1);
                (float? s2Distance, int s2Index) = ClampSegmentsAtOrigin(outerSegment, 0, s2);

                if (shortestCollisionDistance == null || s1Distance < shortestCollisionDistance)
                {
                    closestSegment = s1;
                    pointIndexOfPath = s1Index;
                    shortestCollisionDistance = s1Distance;
                }

                if (s2Distance < shortestCollisionDistance)
                {
                    closestSegment = s2;
                    pointIndexOfPath = s2Index;
                    shortestCollisionDistance = s2Distance;
                }
            }
            
            watch.Stop();

            var elapsedTicks = watch.ElapsedTicks;
        
            Console.WriteLine("No errors. Ran in " + elapsedTicks + " ticks.");
            
        }
        
        // if (StateThreeSnappedSegment != null)
        // {
        //     PathSegment newSegment = segments[segments.Count - 1];
        //     PathSegment clampedSegment = Segments[(int)StateThreeSnappedSegment];
        //     if (StateThreeSnappedPoint == 0)
        //     {
        //         PathSegmentConnectors[(int)clampedSegment.FrontConnector].SegmentIDs.Add(newSegment.ID);
        //         PathSegmentConnectors[(int)clampedSegment.FrontConnector].PointIDs.Add(newSegment.Path.Length - 1);
        //         newSegment.EndConnector = clampedSegment.FrontConnector;
        //     }
        //
        //     if (StateThreeSnappedPoint == clampedSegment.Path.Length - 1)
        //     {
        //         PathSegmentConnectors[(int)clampedSegment.EndConnector].SegmentIDs.Add(newSegment.ID);
        //         PathSegmentConnectors[(int)clampedSegment.EndConnector].PointIDs.Add(newSegment.Path.Length - 1);
        //         newSegment.EndConnector = clampedSegment.EndConnector;
        //     }
        // }        
    }
    
    public void ClipParallelPathsToIntersection(PathSegment sourceRulerSegment, PathSegmentConnector rulerConnector)
    {
        int componentID = ComponentManager.GetComponentID<PathSegment>();
        int o1EntityID = sourceRulerSegment.AdjacentPathSegmentEntities[0];
        int o2EntityID = sourceRulerSegment.AdjacentPathSegmentEntities[1];
        PathSegment o1Segment = (PathSegment)EntityManager.EntityComponents[o1EntityID][componentID];
        PathSegment o2Segment = (PathSegment)EntityManager.EntityComponents[o2EntityID][componentID];
        o1Segment = GetOuterMostSegment(o1Segment, sourceRulerSegment);
        o2Segment = GetOuterMostSegment(o2Segment, sourceRulerSegment);
        
        GetOuterSegmentRayClamp(o1Segment, sourceRulerSegment, P1ClampedSegment);
        
        //SlicePathSegment(o1Segment.ID, (int)o1Segment.Path.Length/3);
        //SlicePathSegment(o2Segment.ID, (int)o2Segment.Path.Length/3);

        //go to outermost parallel path segments from the sourceRulerSegment
        //get outermost path segments that connect to the rulerConnector (and not parallel to sourceRulerSegment)
        //determine if new clamped intersection point is the last index or first index of the path segment 
        //create vector from previous point to last point (or next point to first point) and use that as direction of ray to intersect with origin at next/previous 
        //try one segment at a time
        //check intersection of plane/rectangle from end point to start point, shrinking by one on alternating sides
        //until path points for the plane/rectangle are side by side
        //this can be done in a binary search fashion probably
        //then store this distance of intersection and go to the next overall segment and repeat for all outer paths
        //if ever there is no intersection on both sides of the binary search, the current segment from ruler should have the path point go back/forward by 1
        //finally remove any back/forward points of the path then add one more point to the point of shortest intersection 
        //once the collision points are determined for the outer segments, create a new plane with those 2 points (and down towards planet center)
        //then set each inner segment to end at the collision point of that new plane
    }

    public void GenerateParallelPathSegmentConnectors(PathSegment sourceRulerSegment, PathSegmentConnector rulerConnector)
    {
        
    }

    public void RemovePathSegmentPointsFromOctree(PathSegment segment)
    {
        for (int i = 0; i < segment.Path.Length; i++)
        {
            PathSegmentPointOctreeData octreeData = new PathSegmentPointOctreeData();
            octreeData.PointIndex = i;
            octreeData.EntityID = segment.EntityID;
            Octree.RemoveElementAtLocation(segment.Path[i], octreeData);
        }
    }

    //TODO update this to make sure segments that aren't rulers don't become a ruler after slicing
    //TODO still need to verify that each segment has it's end and front connectors set correctly
    public void SlicePathSegment(int entityIndex, int pathIndex, PathSegment previousPathSegment = null)
    {
        int componentID = ComponentManager.GetComponentID<PathSegment>();
        PathSegment oldSegment = (PathSegment)EntityManager.EntityComponents[entityIndex][componentID];
        RemovePathSegmentPointsFromOctree(oldSegment);
        Vector3[] path = oldSegment.Path;

        
        //Vector3[] path = Segments[segmentIndex].Path;
        //PathSegment oldSegment = Segments[segmentIndex];
        var slicedPathOne = path;
        Array.Resize(ref slicedPathOne, pathIndex);
        oldSegment.Path = slicedPathOne;
        
        Vector3[] newPathTwo = new Vector3[path.Length - pathIndex - 1];
        for (int i = 0; i < newPathTwo.Length; i++)
        {
            newPathTwo[i] = path[pathIndex + i + 1];
        }
        PathSegment segmentTwo = new PathSegment()
        {
            Path = newPathTwo,
            PathDirection = oldSegment.PathDirection,
            IsLaneRuler = oldSegment.IsLaneRuler
        };
        EntityManager.AddEntity();
        EntityManager.AddComponentToEntity<PathSegment>(EntityManager.LastAddedEntity, segmentTwo);
        segmentTwo.ID = Segments.Count;
        segmentTwo.EntityID = EntityManager.LastAddedEntity;
        Segments.Add(segmentTwo);
        
        InsertSegmentPathPointsIntoOctree(segmentTwo);
        InsertSegmentPathPointsIntoOctree(oldSegment);
        
        segmentTwo.EndConnector = oldSegment.EndConnector;
        // if (oldSegment.EndConnector != null)
        // {
            PathSegmentConnectors[(int)oldSegment.EndConnector].SegmentEntities.Add(segmentTwo.EntityID);
            PathSegmentConnectors[(int)oldSegment.EndConnector].PointIDs.Add(segmentTwo.Path.Length - 1);
            PathSegmentConnectors[(int)oldSegment.EndConnector].DebugPoints.Add(new VertexPositionColor(segmentTwo.Path[^1], Color.Blue));
        // }
        //
        // if (oldSegment.EndConnector != null)
        // {
            PathSegmentConnectorSystems.RemoveSegmentFromConnector(PathSegmentConnectors[(int)oldSegment.EndConnector], entityIndex, path.Length - 1);
            //PathSegmentConnectorSystems.RemoveSegmentFromConnector(PathSegmentConnectors[(int)oldSegment.EndConnector], entityIndex, path.Length);

            //PathSegmentConnector testConnector = PathSegmentConnectors[(int)oldSegment.EndConnector];

            // for (int i = 0; i < testConnector.SegmentEntities.Count; i++)
            // {
            //     Console.WriteLine($"Entity: {testConnector.SegmentEntities[i]} Path Point: {testConnector.PointIDs[i]} | Old Path Length: {path.Length}");
            // }
            // Console.WriteLine($"Removed Segment From Connector: {oldSegment.EndConnector} With segment ID: {oldSegment.EntityID}");
        // }

        PathSegmentConnector slicedConnector = new PathSegmentConnector();
        slicedConnector.ID = PathSegmentConnectors.Count;
        PathSegmentConnectors.Add(slicedConnector);
        slicedConnector.Position = path[pathIndex];
        InsertSegmentConnectorIntoOctree(slicedConnector);
        
        PathSegmentConnectorSystems.AddSegmentToSegmentConnector(slicedConnector, oldSegment, SegmentConnectorIndex.Last);
        PathSegmentConnectorSystems.AddSegmentToSegmentConnector(slicedConnector, segmentTwo, SegmentConnectorIndex.First);

        if (oldSegment.AdjacentPathSegmentEntities[0] != 0)
        {
            
        }

        if (oldSegment.AdjacentPathSegmentEntities[1] != 0)
        {
            
        }
    }

    public static float AngleBetweenThreePoints(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 worldCenter)
    {
        Vector3 upVector = p1 - worldCenter;
        var v1 = p2 - p1;
        var v2 = p3 - p2;

        var cross = Vector3.Cross(v1, v2);
        var dot = Vector3.Dot(v1, v2);

        var angle = Math.Atan2(cross.Length(), dot);

        var test = Vector3.Dot(upVector, cross);
        if (test < 0.0) angle = -angle;
        return (float) angle;
    }

    public bool ClampCursorToRoad()
    {
        return false;
    }
    //Vector3 angleToRoad = Vector3.Zero;
    public bool ClampToExistingRoadPoint()
    {
        int cursorID = ComponentManager.GetComponentID<Cursor>();
        Cursor cursor = (Cursor)EntityManager.EntityComponents[0][cursorID];

        bool connected = false;

        if (cursor.Location == null)
        {
            return connected;
        }

        //TODO optimize this
        //Could put points in an octree or something
        foreach (PathSegment segment in Segments)
        {
            float closestPoint = 100;

            for (int i = 0; i < segment.Path.Length; i++)
            {
                if (PendingP2 != Vector3.Zero && PendingP1 != Vector3.Zero)
                {
                    Vector3 point = segment.Path[i];
                    float distance = Vector3.Distance(point, (Vector3)cursor.Location);
                    if (distance < 0.25f && distance < closestPoint)
                    {
                        //Console.WriteLine(closestPoint);
                        closestPoint = distance;
                        cursor.Location = point;

                        ClampedSegment = segment;
                        P1ClampedSegmentPathIndex = i;

                        if (i < segment.Path.Length - 1)
                        {
                            ClampedPointForAngle = segment.Path[i + 1];
                        } else {
                            ClampedPointForAngle = segment.Path[i - 1];
                        }

                        connected = true;
                    }
                    //Console.WriteLine(roadAngle);
                }
                //if (PendingP2 == Vector3.Zero && PendingP3 == Vector3.Zero)
                if (!awaitingP2)
                {
                    Vector3 point = segment.Path[i];
                    if (Vector3.Distance(point, (Vector3)cursor.Location) < 0.5f)
                    {
                        if (!(i == 0 || i == segment.Path.Length - 1))
                        {
                            //cursor.brushLocation = point;
                            continue;
                        }
                        StartingConnectingCursorPoint = point;
                        connected = true;
                        cursor.Location = point;

                        float p1ClosestDistance = 100;
                        float p2ClosestDistance = 100;

                        if (i < segment.Path.Length - 1)
                        {
                            ClampedPointForAngle = segment.Path[i + 1];
                        } else {
                            ClampedPointForAngle = segment.Path[i - 1];
                        }

                        ClampedSegment = segment;
                        P1ClampedSegmentPathIndex = i;
                    }
                }
            }
        }

        return connected;
    }

    public static int[] GenerateIndices(Vector3[] vertices)
    {
        int[] indices = new int[(vertices.Length - 2) * 3];

        int counter = 0;

        for (int i = 0; i < vertices.Length - 2; i+=2)
        {
            indices[counter++] = i + 2;
            indices[counter++] = i + 3;
            indices[counter++] = i;

            indices[counter++] = i + 1;
            indices[counter++] = i;
            indices[counter++] = i + 3;
        }

        return indices;
    }

    public static void GenerateUVs(Vector3[] vertices)
    {

    }

    //vertices and triangles must be the same length and are relative to one another at the same index
    //triangles will only need the first index and can parse the remaining points by adding to the triangle point index
    public void ClampTrianglesToRoad(List<Vector3> verticesOfRoad, List<int[]> trianglesCollided, Terrain terrain)
    {

        //currently this is being applied after the vertices are clamped to the terrain
        //It will probably be better to clamp the bezier points to the terrain and level the terrain around the bezier points instead
        //and then clamp the road vertices to the terrain

        for (int i = 0; i < verticesOfRoad.Count; i ++)
        {
            Vector3 vertex = verticesOfRoad[i];
            //Vector3 collisionPoint = collisionPoints[i];
            int[] triangles = trianglesCollided[i];

            float vertexDistanceFromCenter = Vector3.Distance(vertex, terrain.WorldCenter);

            Vector3 p1 = terrain.Vertices[terrain.Indices[triangles[0]]].Position;
            Vector3 p2 = terrain.Vertices[terrain.Indices[triangles[1]]].Position;
            Vector3 p3 = terrain.Vertices[terrain.Indices[triangles[2]]].Position;

            Vector3 dir1 = p1 - terrain.WorldCenter;
            dir1.Normalize();
            Vector3 dir2 = p2 - terrain.WorldCenter;
            dir2.Normalize();
            Vector3 dir3 = p3 - terrain.WorldCenter;
            dir3.Normalize();

            terrain.Vertices[terrain.Indices[triangles[0]]].Position = dir1 * vertexDistanceFromCenter;
            terrain.Vertices[terrain.Indices[triangles[1]]].Position = dir2 * vertexDistanceFromCenter;
            terrain.Vertices[terrain.Indices[triangles[2]]].Position = dir3 * vertexDistanceFromCenter;
        }
        //should grab closest vertices to the road? maybe include the list of collision points? and use that to get more vertices close the road and
        //will need to clamp the triangle(s) to closest vertex
        //could skip vertices that share the same triangle(s) but only based on
        //if vertices aren't skipped that might be okay, just more expensive but can test it first
        //grab close triangles, not just the triangle intersected by ray from road points/vertex points
    }

    //TODO
    //this is pretty slow for updating a large number of vertices, need to see about optimizing this
    //triangles HashSet must be instantiated
    // public static Vector3[] ClampVerticesToTerrain(Vector3[] vertices, Terrain terrain, ArcBallCamera camera, GraphicsDevice graphicsDevice, HashSet<int> triangles)
    // {
    //     for (int i = 0; i < vertices.Length; i++)
    //     {
    //         Vector3 direction = vertices[i] - terrain.WorldCenter;
    //         direction.Normalize();

    //         Vector3? pointIntersected = null;
    //         Ray ray = new Ray(direction * 500, -direction);
    //         List<Octree> nodes = terrain.Collider.GetIntersectedNodes(camera, graphicsDevice, ray);
    //         int?[] triangle = terrain.Collider.GetCollidedTriangleFromNodes(ray, nodes, terrain, out pointIntersected);

    //         if (triangle == null)
    //             continue;

    //         int[] result = Array.ConvertAll(triangle, x => x ?? 0);
    //         triangles.Add(result[0]);

    //         vertices[i] = (Vector3)pointIntersected + direction * .25f;
    //     }

    //     return vertices;
    // }

    public static Vector3[] ClampVerticesToTerrain(Vector3[] vertices, Terrain terrain, ArcBallCamera camera, GraphicsDevice graphicsDevice)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = vertices[i] + new Vector3 (0, 0.1f, 0); //small offset to beat z-fighting of terrain
        }

        return vertices;
    }

    public static Vector3[] GenerateVertices(Vector3[] bezierPoints, Vector3 worldCenter, PathSegment connectingSegment = null, float width = 0.25f)
    {
        Vector3[] vertices = new Vector3[bezierPoints.Length * 2];
        for (int i = 0; i < bezierPoints.Length; i++)
        {
            Vector3 u;
            Vector3 v;

            float directionModifier = 1;

            if (i == bezierPoints.Length - 1)
            {
                u = bezierPoints[i - 1] - bezierPoints[i];
                v = worldCenter - bezierPoints[i];
            }
            else if (i == 0 && connectingSegment == null)
            {
                u = bezierPoints[i + 1] - bezierPoints[i];
                v = worldCenter - bezierPoints[i];
                directionModifier = -1;
            }
            else if (i == 0 && connectingSegment != null)
            {
                Vector3 dir1 = connectingSegment.Path[connectingSegment.Path.Length - 2] - bezierPoints[i];
                Vector3 dir2 = bezierPoints[i] - bezierPoints[i];
                Vector3 p1 = bezierPoints[i];
                Vector3 p2 = bezierPoints[i] + ((dir1 + dir2) / 2);
                Vector3 p3 = worldCenter;
                u = p2 - p1;
                v = p3 - p1;
            }
            else
            {
                Vector3 dir1 = bezierPoints[i - 1] - bezierPoints[i];
                Vector3 dir2 = bezierPoints[i] - bezierPoints[i + 1];
                Vector3 p1 = bezierPoints[i];
                Vector3 p2 = bezierPoints[i] + ((dir1 + dir2) / 2);
                Vector3 p3 = worldCenter;
                u = p2 - p1;
                v = p3 - p1;
            }
            Vector3 vectorNormal = Vector3.Cross(u, v);
            vectorNormal.Normalize();
            vertices[i * 2] = bezierPoints[i] - vectorNormal * width * directionModifier;
            vertices[i * 2 + 1] = bezierPoints[i] + vectorNormal * width * directionModifier;
        }

        return vertices;
    }

    //this calculates 2 symmetric control points for a cubic bezier curve based on a single point
    //the constant controls the tightness of the curvature by extending or shortening the distance of the calculated control points to the original single control point
    //we choose 0.55228 because it gives a close approximation of a circle made with 4 cubic bezier curves
    //the actual constant would be 4(sqrt(2) - 1)/3 for a quarter of a circle
    public Tuple<Vector3, Vector3> CalculateControlPoints(Vector3 p0, Vector3 p1, Vector3 controlPoint)
    {
        float constant = -0.55228f;
        float cp1Distance = Vector3.Distance(p0, controlPoint);
        Vector3 cp1Dir = p0 - controlPoint;
        cp1Dir.Normalize();
        Vector3 cp1 = p0 + cp1Dir * constant * cp1Distance;

        float cp2Distance = Vector3.Distance(p1, controlPoint);
        Vector3 cp2Dir = p1 - controlPoint;
        cp2Dir.Normalize();
        Vector3 cp2 = p1 + cp2Dir * constant * cp2Distance;

        return new Tuple<Vector3, Vector3>(cp1, cp2);

    }

    public Vector3[] CalculatePoints(Vector3 p0, Vector3 p1, Vector3 cp0, Vector3 cp1, int resolution = 1)
    {
        Vector3[] points = new Vector3[resolution + 1];

        for (int i = 0; i < resolution + 1; i++)
        {
            float interval = 1.0f / resolution * i;
            points[i] = GenerateBezierPoint(p0, p1, cp0, cp1, interval);
        }

        return points;
    }

    public Vector3[] CalculateEvenlySpacedPoints(float spacing, Vector3 p0, Vector3 p1, Vector3 cp0, Vector3 cp1, float resolution = 1)
    {
        List<Vector3> evenlySpacedPoints = new List<Vector3>();
        evenlySpacedPoints.Add(p0);
        Vector3 previousPoint = p0;
        float dstSinceLastEvenPoint = 0;

        //float chord = (p0 - p1).Length();
        //float controlNet = (p0 - cp0).Length() + (cp1 - cp0).Length() + (p1 - cp1).Length();
        //float estArcLength = (controlNet + chord) / 2;

        for (int i = 0; i < resolution + 1; i++)
        {
            float interval = 1.0f / resolution * i;
            Vector3 pointOnCurve = GenerateBezierPoint(p0, p1, cp0, cp1, interval);
            dstSinceLastEvenPoint += Vector3.Distance(previousPoint, pointOnCurve);

            while (dstSinceLastEvenPoint >= spacing)
            {
                float overshootDst = dstSinceLastEvenPoint - spacing;
                Vector3 overshootDir = previousPoint - pointOnCurve;
                overshootDir.Normalize();
                Vector3 newEvenlySpacedPoint = pointOnCurve + overshootDir * overshootDst;
                evenlySpacedPoints.Add(newEvenlySpacedPoint);
                dstSinceLastEvenPoint = overshootDst;
                previousPoint = newEvenlySpacedPoint;
            }

            previousPoint = pointOnCurve;
        }

        evenlySpacedPoints.Add(p1);
        return evenlySpacedPoints.ToArray();

    }

    //this is generating a cubic bezier curve.
    public static Vector3 GenerateBezierPoint(Vector3 p0, Vector3 p1, Vector3 cp0, Vector3 cp1, float interval)
    {
            Vector3 point = MathF.Pow(1 - interval, 3) * p0 + 3 * MathF.Pow(1-interval, 2) * interval * cp0 + 3 * (1-interval) * MathF.Pow(interval, 2) * cp1 + MathF.Pow(interval,3) * p1;
            return point;
    }

    public Vector3 PendingP1 = Vector3.Zero;
    public Vector3 PendingP2  = Vector3.Zero;
    public Vector3 PendingP3 = Vector3.Zero;
    public void SelectPointStraight(Vector3 selectedPoint, GraphicsDevice graphicsDevice, Terrain terrain, ArcBallCamera camera)
    {
        if (PendingP1 == Vector3.Zero)
        {
            PendingP1 = selectedPoint;
            //Console.WriteLine("P1 Set: " + PendingP1);
        } else if (PendingP3 == Vector3.Zero) {
            PendingP3 = selectedPoint;
            PendingP2 = Vector3.Lerp(PendingP3, PendingP1, 0.5f);
            //Console.WriteLine("P2 Set: " + PendingP2);
            //Console.WriteLine("P3 Set: " + PendingP3);
        }
    }
    public void SelectPoint(Vector3 selectedPoint, GraphicsDevice graphicsDevice, Terrain terrain, ArcBallCamera camera)
    {
        int componentID = ComponentManager.GetComponentID<PathSegment>();

        if (PendingP1 == Vector3.Zero)
        {
            if (StateOneSnappedPoint != null)
            {
                if (!ClampedPointOctreeData.Connector)
                {
                    PathSegment stateOneSegment = (PathSegment)EntityManager.EntityComponents[(int)StateOneSnappedSegmentEntity][componentID];
                    if (ClampedPointIsInvalid((int)StateOneSnappedPoint, stateOneSegment.Path.Length))
                    {
                        StateOneSnappedPoint = null;
                        StateOneSnappedSegmentEntity = null;
                        return;
                    }   
                }
                else
                {
                    StateOneSnappedSegmentConnector = ClampedPointOctreeData.EntityID;
                }
            }
            PendingP1 = selectedPoint;
            state = State.Two;
        } else if (PendingP2 == Vector3.Zero) {
            // clampedRoadAngle = roadAngle;
            PendingP2 = selectedPoint;
            state = State.Three;
        } else if (PendingP3 == Vector3.Zero) {
            if (StateThreeSnappedPoint != null)
            {
                if (!ClampedPointOctreeData.Connector)
                {
                    PathSegment stateOneSegment = (PathSegment)EntityManager.EntityComponents[(int)StateThreeSnappedSegmentEntity][componentID];
                    if (ClampedPointIsInvalid((int)StateThreeSnappedPoint, stateOneSegment.Path.Length))
                    {
                        StateThreeSnappedPoint = null;
                        StateThreeSnappedSegmentEntity = null;
                        return;
                    }
                }
                else
                {
                    StateThreeSnappedSegmentConnector = ClampedPointOctreeData.EntityID;
                }
            }
            PendingP3 = selectedPoint;
        }
    }

    public void Reset()
    {
        PendingP1 = Vector3.Zero;
        PendingP2 = Vector3.Zero;
        PendingP3 = Vector3.Zero;
        state = State.One;
        p1Clamped = false;
        p3Clamped = false;
        ClampedSegment = null;
        StateOneSnappedPoint = null;
        StateOneSnappedSegmentEntity = null;
        StateOneSnappedSegmentConnector = null;
        StateThreeSnappedPoint = null;
        StateThreeSnappedSegmentEntity = null;
        StateThreeSnappedSegmentConnector = null;
    }

    //could use some optimizing. Not sure of many ideas aside from an octree
    // public bool PointClipsExistingRoad(Vector3 point)
    // {
    //     foreach (PathSegment segment in Segments)
    //     {
    //         for (int i = 0; i < segment.Indices.Length; i += 3)
    //         {
    //             Vector3 p1 = segment.Vertices[segment.Indices[i]].Position;
    //             Vector3 p2 = segment.Vertices[segment.Indices[i + 1]].Position;
    //             Vector3 p3 = segment.Vertices[segment.Indices[i + 2]].Position;
    //
    //             float? intersectPoint;
    //             Vector3 rayDir = point;
    //             rayDir.Normalize();
    //             Ray ray = new Ray(Vector3.Zero, rayDir);
    //             AOneMath.RayIntersectsTriangle(ref ray, ref p1, ref p2, ref p3, out intersectPoint);
    //
    //             if (intersectPoint != null)
    //             {
    //                 return true;
    //             }
    //         }
    //     }
    //
    //     return false;
    // }

    // public bool OutlineMeshClipsExistingRoad(PathSegment temporaryMesh)
    // {
    //     for (int i = 0; i < temporaryMesh.Vertices.Length; i ++)
    //     {
    //         if (PointClipsExistingRoad(temporaryMesh.Vertices[i].Position))
    //         {
    //             return true;
    //         }
    //     }
    //
    //     return false;
    // }

    //public bool ClampCursorToExistingPathPoint(Cursor cursor, out int segmentIndex, out int pointIndex, float clampDistance = 1f)
    public bool ClampCursorToExistingPathPoint(Cursor cursor, out int segmentEntityID, out int pointIndex, float clampDistance = 1f)
    {
        //TODO need to add in code here for the new octree
        float? shortestDistance = null;
        //segmentIndex = 0;
        segmentEntityID = 0;
        pointIndex = 0;
        Vector3? snappedLocation = null;
        PathSegmentPointOctreeData closestOctreePoint = null;

        if (cursor.Location == null)
            return false;

        List<OctreeElement<PathSegmentPointOctreeData>> firstElements = Octree.GetElementListsFromSphere((Vector3)cursor.Location, clampDistance);

        //Console.WriteLine("First Elements: " + firstElements.Count);
        int componentId = ComponentManager.GetComponentID<PathSegment>();
        
        for (int i = 0; i < firstElements.Count; i++)
        {
            OctreeElement<PathSegmentPointOctreeData> element = firstElements[i];

            int testCounter = 0;

            while (true)
            {
                //Console.WriteLine("Am I in an infinite loop?");
                float distance = Vector3.Distance(element.Data.Position, (Vector3)cursor.Location);
                //if (distance > clampDistance) continue;
                
                if ((shortestDistance == null || distance < shortestDistance) && distance <= clampDistance)
                {
                    shortestDistance = distance;
                    //PathSegment segment = (PathSegment)EntityManager.EntityComponents[element.Data.EntityID][componentId];
                    segmentEntityID = element.Data.EntityID;
                    //segmentIndex = segment.
                    pointIndex = element.Data.PointIndex;
                    snappedLocation = element.Data.Position;
                    closestOctreePoint = element.Data;
                }
                
                if (element.NextElement == null) 
                    break;
                element = element.NextElement;
            }
        }

        //  for (int i = 0; i < Segments.Count; i++)
        //  {
        //      PathSegment segment = Segments[i];
        //      if (!segment.IsLaneRuler)
        //          continue;
        //     
        //      for (int j = 0; j < segment.Path.Length; j++)
        //      {
        //          float distance = Vector3.Distance(segment.Path[j], (Vector3)cursor.Location);
        //     
        //          if (distance > clampDistance)
        //          {
        //              continue;
        //          }
        //          // Console.WriteLine("Checking Segment: " + i);
        //          // Console.WriteLine("Checking Point: " + j);
        //          // Console.WriteLine("Distance: " + distance);
        //     
        //          if (shortestDistance == null || distance < shortestDistance)
        //          {
        //              shortestDistance = distance;
        //              segmentIndex = i;
        //              pointIndex = j;
        //          }
        //      }
        // }

        //List<OctreeElement<PathSegmentOctreeData>> linkedInitialElements = Octree.GetElementListsFromSphere((Vector3)cursor.Location, 10);
        //OctreeElement<PathSegmentOctreeData> linkedInitialElements = Octree.GetElementListFromLocation((Vector3)cursor.Location);

        
        //if (linkedInitialElements != null) 
        //    Console.WriteLine("Found Elements!");

        ClampedPointOctreeData = closestOctreePoint;

        if (shortestDistance != null)
        {
            //cursor.Location = Segments[segmentIndex].Path[pointIndex];
            cursor.Location = snappedLocation;
            return true;
        }
        
        return false;
    }

    public void InsertSegmentPathPointsIntoOctree(PathSegment segment)
    {
        for (int i = 0; i < segment.Path.Length; i++)
        {
            PathSegmentPointOctreeData octreeData = new PathSegmentPointOctreeData();
            octreeData.EntityID = segment.EntityID;
            octreeData.PointIndex = i;
            octreeData.Position = segment.Path[i];
            Octree.InsertElementToOctreeFromPoint(octreeData.Position, octreeData); 
            //Console.WriteLine("Inserted: " + i);
        }
    }

    private void InsertSegmentConnectorIntoOctree(PathSegmentConnector connector)
    {
        PathSegmentPointOctreeData octreeData = new PathSegmentPointOctreeData();
        octreeData.EntityID = connector.ID;
        octreeData.Position = connector.Position;
        octreeData.Connector = true;
        Octree.InsertElementToOctreeFromPoint(octreeData.Position, octreeData); 
    }

    public void Generate(GraphicsDevice graphicsDevice, Terrain terrain, ArcBallCamera camera, bool p3Clamped)
    {
        if (PendingP3 == Vector3.Zero)
        {
            return;
        }

        List<PathSegment> segments = GeneratePath(graphicsDevice, PendingP1, PendingP3, PendingP2, terrain, camera);
        
        if (segments == null)
        {
            Reset();
            return;
        }
        
        foreach (PathSegment segment in segments)
        {
            InsertSegmentPathPointsIntoOctree(segment);
            GenerateRibbonMesh(segment);
        }
        
        //PathSegmentSystems.IntersectAndRemoveZones(segments);
        
        //uncomment to enable path tracing for the segments/connectors
        //PathSegmentConnectorSystems.DebugWriteSegmentPathTrace(PathSegmentConnectors, PathSegmentConnectors[0]);
        Random rand = new Random();
        PathSegmentConnector origin = PathSegmentConnectors[rand.Next(PathSegmentConnectors.Count)];
        
        //TODO can probably rewrite this or add it in later
        //RoadMeshUtilities.CalculatePaths(origin, this);

        //uncomment below code for debugging path
        // foreach (PathSegment segment in Segments)
        // {
        //     Console.WriteLine($"Segment ID: {segment.EntityID}");
        //     Console.WriteLine($"Front Connector: {segment.FrontConnector}");
        //     Console.WriteLine($"End Connector: {segment.EndConnector}");
        //     Console.WriteLine();
        // }
        //
        // foreach (PathSegmentConnector connector in PathSegmentConnectors)
        // {
        //     Console.WriteLine($"Connector ID: {connector.ID}");
        //     Console.WriteLine($"Connected Segments: ");
        //     foreach (int segmentID in connector.SegmentEntities)
        //     {
        //         Console.WriteLine(segmentID);
        //     }
        //     Console.WriteLine();
        // }
        
        //InsertSegmentPathPointsIntoOctree(segments);
        //GenerateParallelPaths(segments, 1, 1);
        
        //when generating, if the roadpath is 1, it should automatically be one way and the path itself should just be
        //the ruler path

        // foreach (PathSegment segment in segments)
        // {
        //     EntityManager.AddEntity();
        //     int segmentEntity = EntityManager.LastAddedEntity;
        //     EntityManager.AddComponentToEntity<PathSegment>(segmentEntity, segment);
        //     Segments.Add(segment);
        //     segment.EntityID = segmentEntity;
        //     //Console.WriteLine("Generating Segment Zones");
        //     //ZoneGenerator.GenerateZone(segment.Path);
        // }
        
        //Task.Run(() => ZoneGenerator.GenerateZone(segments));
        
        Reset();
    }

    //TODO when creating segments here, need to differentiate left from right I think
    private List<PathSegment> GenerateParallelPath(List<PathSegment> rulerSegments, int pathOffset, float pathWidth, bool leftOfCenter, List<PathSegment> previousPath = null)
    {
        int directionMod = leftOfCenter == true ? -1 : 1;
        List<int> segmentEntities = ComponentManager.GetComponent<PathSegment>();
        List<PathSegment> newSegments = new List<PathSegment>(); 
        for (int i = 0; i < rulerSegments.Count; i++)
        {
            Vector3[] segmentPoints = new Vector3[rulerSegments[i].Path.Length];
            for (int j = 0; j < rulerSegments[i].Path.Length; j++)
            {
                Vector3 currentPoint = rulerSegments[i].Path[j];
                Vector3 perpendicular = rulerSegments[i].Perpendiculars[j];

                segmentPoints[j] = currentPoint + (directionMod * perpendicular * pathWidth * .5f) + (directionMod * perpendicular * pathOffset * pathWidth);
            }
            //Console.WriteLine(segmentEntities.Count);
            
            PathSegment newSegment = new PathSegment()
            {
                ID = segmentEntities.Count,
                IsLaneRuler = false,
                PathDirection = (PathDirection)directionMod
            };
            newSegment.Path = segmentPoints;

            newSegments.Add(newSegment);
            EntityManager.AddEntity();
            int segmentEntity = EntityManager.LastAddedEntity;
            EntityManager.AddComponentToEntity<PathSegment>(segmentEntity, newSegment);
            Segments.Add(newSegment);
            newSegment.EntityID = segmentEntity;

            int rulerIndex = 0;
            if (!leftOfCenter)
                rulerIndex = 1;

            if (previousPath == null)
            {
                rulerSegments[i].AdjacentPathSegmentEntities[rulerIndex] = newSegment.EntityID;
                newSegment.AdjacentPathSegmentEntities[0] = rulerSegments[i].EntityID;
            }
            else
            {
                previousPath[i].AdjacentPathSegmentEntities[1] = newSegment.EntityID;
                newSegment.AdjacentPathSegmentEntities[0] = previousPath[i].EntityID;
            }
        }

        //TODO need something different here I think to fix this lol
        //GeneratePathSegmentConnectors(newSegments);
        return newSegments;
    }
    
    //TODO consider putting this into the RoadRibbonMesh class, might make more sense there
    public void GenerateRibbonMesh(PathSegment segment)
    {
        Vector3[] perpendiculars = GeneratePerpendiculars(segment.Path);
        Vector3[] meshVerts = new Vector3[segment.Path.Length * 2];
        for (int i = 0; i < segment.Path.Length; i++)
        {
            meshVerts[2 * i] = segment.Path[i] + perpendiculars[i] * PATH_WIDTH + new Vector3(0, 0.1f, 0);
            meshVerts[2 * i + 1] = segment.Path[i] - perpendiculars[i] * PATH_WIDTH + new Vector3(0, 0.1f, 0);
        }
        
        _ribbonMesh.Insert(meshVerts);
    }

    private void GenerateParallelPaths(List<PathSegment> rulerSegments, int leftPathCount, int rightPathCount)
    {
        
        //TODO here we need to link the segments to their neighbor segments
        //The count of the "rulerSegments" list is the number of segment entities added
        //The parallel segment will just be the offset segment from the count
        
        //Parallel paths are generated from the inside out
        //can't naively set the parallel entity IDs here by an offset
            //if there is a dead entity that ID will be replaced
            //might be best to "reserve" entities after generating the ruler
            //path and give then provide a list of entityIDs to use for each parallel path
        
        //TODO need to connect the segments together
        //can ignore any adjacent segments that have an entity of 0 since this is the global entity and we know it can't be a road segment
        //only segments with a specific direction can connect to each other based on the point they are connecting from 
            //segments connect to the same segment direction when the path points are also opposite (final and first)
            //segments with dir -1 and 1 connect to the opposite lane when path points are both the final or both the first
                //these rules may apply to the intersections as well
                //left turn intersections need to be offset so that 2 cars can turn at the same time from opposite lanes, they need to pass on the inside of the center of the lane
        //lanes should only turn into another direction once, lanes can have multiple turns, but only one per connected segment group
            //could potentially do a distance check to see which lanes are furthest? might be a bit of a hack but would probably work
            //we start with the ruler segment since that's what we will collide with/snap to
            //grab an adjacent segment from both the new ruler and the one that is clamped
                //if these adjacent segments can connect, check if the connection is opposite path points, or same.
                    //if same, follow adjacent segments for both selected segments until no more left and then connect out segments first
                    //if different, connect inner most segments
                    //if different (left turn), turn either needs to only turn left, or no other adjacent lanes can turn left in front of it if it can also go straight
        
        //build left paths
        List<PathSegment> lastLeftSegments = null;
        for (int i = 0; i < leftPathCount; i++)
        {
            if (i > 0)
                lastLeftSegments = GenerateParallelPath(rulerSegments, i, 0.25f, true, lastLeftSegments);
            else
                lastLeftSegments = GenerateParallelPath(rulerSegments, i, 0.25f, true);
        }

        List<PathSegment> lastRightSegments = null;
        for (int i = 0; i < rightPathCount; i++)
        {
            if (i > 0)
                lastRightSegments = GenerateParallelPath(rulerSegments, i, 0.25f, false, lastRightSegments);
            else
                lastRightSegments = GenerateParallelPath(rulerSegments, i, 0.25f, false);
        }
        
        //TODO remove this just testing outer spath selection
        //ClipParallelPathsToIntersection(rulerSegments[0], null);
    }

    //TODO might want to use this in the future for generating paths with multiple lanes
    public static Vector3[] GeneratePerpendiculars(Vector3[] path)
    {
        Vector3[] perpendiculars = new Vector3[path.Length];
        for (int j = 0; j < path.Length; j++)
        {
            Vector3 p1 = path[j];
            Vector3 p2;
            if (j == path.Length - 1)
            {
                p1 = path[j - 1];
                p2 = path[j];
            }
            else
            {
                p2 = path[j + 1];
            }
            perpendiculars[j] = Calculate3DTangentNormal(p1, p2);
        }

        return perpendiculars;
    }

    //TODO test out the above function for generating perpendiculars first, it might work okay. If it doesn't,
        //might need to consider creating perpendiculars for each segment with the endpoints considering the first point in the next segment
    // public void GeneratePerpendiculars(List<PathSegment> segments)
    // {
    //     for (int i = 0; i < segments.Count; i++)
    //     {
    //         PathSegment segment = segments[i];
    //         Vector3[] perpendiculars = new Vector3[segment.Path.Length];
    //         for (int j = 0; j < segment.Path.Length; j++)
    //         {
    //             Vector3 p1 = segment.Path[j];
    //             Vector3 p2;
    //             if (j == segment.Path.Length - 1)
    //             {
    //                 if (i == segments.Count - 1)
    //                 {
    //                     p2 = segment.Path[j - 2];
    //                 }
    //                 else
    //                 {
    //                     p2 = segments[i + 1].Path[0];
    //                 }
    //             }
    //             else
    //             {
    //                 p2 = segment.Path[j + 1];
    //             }
    //
    //             perpendiculars[j] = ZoneGenerator.Calculate3DTangentNormal(p1, p2);
    //         }
    //     }
    // }

    public void UpdateRoadAngle(float angle)
    {
        if (Math.Abs(angle) > Math.PI * .75f)
        {
            roadAngle = RoadAngle.North;
        } else if (Math.Abs(angle) < Math.PI * .4f)
        {
            roadAngle = RoadAngle.South;
        } else if (angle < 0)
        {
            roadAngle = RoadAngle.West;
        } else {
            roadAngle = RoadAngle.East;
        }
    }

    private bool spacePressed;
    private Vector3? previousBrushLocation = null;
    private bool awaitingP2 = false;
    private bool p1Clamped = false;
    private bool p3Clamped = false;

    /// <summary>
    /// Initial update phase where the first point of the road segment to be created can be chosen
    /// </summary>
    public void StateOneUpdate(Cursor cursor)
    {
        if (state != State.One)
            return;

        if(ClampCursorToExistingPathPoint(cursor, out int segmentIndex, out int pointIndex, 1f))
        {
            //StateOneSnappedSegmentEntity = Segments[segmentIndex].EntityID;
            StateOneSnappedSegmentEntity = segmentIndex;
            StateOneSnappedPoint = pointIndex;
        }
        else
        {
            StateOneSnappedPoint = null;
            StateOneSnappedSegmentEntity = null;
        }
    }

    /// <summary>
    /// Secondary update phase where the second point of the road segment that serves as the control point
    /// to control the curve can be chosen
    /// </summary>
    public void StateTwoUpdate()
    {
        if (state != State.Two)
            return;
    }

    /// <summary>
    /// Final update phase where the end point for the road segment can be chosen and the segment will then
    /// be created
    /// </summary>
    public void StateThreeUpdate(Cursor cursor)
    {
        if (state != State.Three)
            return;

        if(ClampCursorToExistingPathPoint(cursor, out int segmentIndex, out int pointIndex, 1f))
        {
            //StateThreeSnappedSegmentEntity = Segments[segmentIndex].EntityID;
            StateThreeSnappedSegmentEntity = segmentIndex;
            StateThreeSnappedPoint = pointIndex;
        }
        else
        {
            StateThreeSnappedPoint = null;
            StateThreeSnappedSegmentEntity = null;
        }
    }
    
    private bool ClampedPointIsInvalid(int pointIndex, int segmentLength)
    {
        //Console.WriteLine("Invalid Clamped Point");
        return (pointIndex <= 1) || (pointIndex >= segmentLength - 2);
    }

    public void OutlineMeshUpdate(Cursor cursor, GraphicsDevice graphicsDevice, Terrain terrain, ArcBallCamera camera)
    {
        if (state == State.One)
        {
            return;
        }

        if (state == State.Two)
        {
            OutlineSegment = GenerateOutlineMesh(graphicsDevice, PendingP1, (Vector3)cursor.Location, Vector3.Lerp((Vector3)cursor.Location, PendingP1, 0.5f), terrain, camera);
        }

        if (state == State.Three)
        {
            OutlineSegment = GenerateOutlineMesh(graphicsDevice, PendingP1, (Vector3)cursor.Location, PendingP2, terrain, camera);
        }

        // if (p3Clamped)
        // {
        //     OutlineSegment = GenerateOutlineMesh(graphicsDevice, PendingP1, p3ClampedPoint, PendingP2, terrain, camera);
        // } else if (PendingP2 != Vector3.Zero && PendingP2 != cursor.Location)
        // {
        // } else if (PendingP1 != cursor.Location) {
        // }
    }

    public void Update(GraphicsDevice graphicsDevice, Terrain terrain, ArcBallCamera camera, KeyboardState keyState)
    {
        if (!CreateEnabled)
            return;
        
        int cursorID = ComponentManager.GetComponentID<Cursor>();
        Cursor cursor = (Cursor)EntityManager.EntityComponents[0][cursorID];

        bool connected = false;

        StateOneUpdate(cursor);
        StateThreeUpdate(cursor);

        if (spacePressed == false && keyState.IsKeyDown(Keys.Space))
        {
            spacePressed = true;
            if (cursor.Location != null)
            {
                SelectPoint((Vector3)cursor.Location, graphicsDevice, terrain, camera);
                //SelectPointStraight((Vector3)terrainCursor.brushLocation, graphicsDevice, terrain, camera);
            }
        }

        OutlineMeshUpdate(cursor, graphicsDevice, terrain, camera);

        // bool isClamped = ClampToExistingRoadPoint();
        // if (isClamped && PendingP1 != Vector3.Zero && !awaitingP2)
        // {
        //     p1Clamped = true;
        //     PendingP1 = Vector3.Zero;
        //     //SelectPointStraight((Vector3)StartingConnectingCursorPoint, graphicsDevice, terrain, camera);
        //     SelectPoint((Vector3)StartingConnectingCursorPoint, graphicsDevice, terrain, camera);
        // }

        // if (PendingP1 != Vector3.Zero && PendingP2 == Vector3.Zero)
        // {
        //     awaitingP2 = true;
        // }

        //The following block of code clamps the length to the edge of the road to the
        //position where the road is intersected. Currently only works with the
        //outline mesh and should also work when the mesh is confirmed
        // if (PendingP3 == Vector3.Zero)
        // {
        //     p3Clamped = (isClamped && PendingP3 == Vector3.Zero && PendingP2 != Vector3.Zero);
        // }
        // if (p3Clamped)
        // {
        //     //float angle;
        //
        //     OutlineSegment = GenerateOutlineMesh(graphicsDevice, PendingP1, (Vector3)cursor.Location, PendingP2, terrain, camera);
        //     //CalculateP3ClampedPoint();
        //     //angle = AngleBetweenThreePoints(p3ClampedPoint, ClampedPointForAngle, (Vector3)cursor.Location, game.terrain.WorldCenter);
        //     //UpdateRoadAngle(angle);
        //     //Console.WriteLine("Angle: " + angle);
        // }

        // if (awaitingP2 && p1Clamped)
        // {
        //     float angle;
        //     //could improve the accuracy by casting all points of the triangle to the same plane
        //     angle = AngleBetweenThreePoints(PendingP1, ClampedPointForAngle, (Vector3)cursor.Location, game.terrain.WorldCenter);
        //     UpdateRoadAngle(angle);
        // }

        // if (meshGenerated)
        // {
        //     ClipsExistingRoad(OutlineSegment);
        //     // if (ClipsExistingRoad(OutlineSegment))
        //     //Console.WriteLine("Clipping!!!!");
        // }

        Generate(graphicsDevice, terrain, camera, p3Clamped);

        // if (spacePressed == false && keyState.IsKeyDown(Keys.Enter))
        // {
        //     spacePressed = true;
        //     terrain.Erode(graphicsDevice);
        // }

        if (spacePressed && keyState.IsKeyUp(Keys.Space))
        {
            spacePressed = false;
        }
        
        _ribbonMesh.Update();

        // if (previousBrushLocation == cursor.Location && !p3Clamped)
        // {
        //     return;
        // }
        //
        // previousBrushLocation = cursor.Location;
    }

    BasicEffect basicEffect;
    public void Draw(GraphicsDevice graphicsDevice, Matrix viewMatrix, Matrix projectionMatrix)
    {
        _ribbonMesh.Draw();
        
        RasterizerState rs = new RasterizerState();
        rs.CullMode = CullMode.CullCounterClockwiseFace;
        //rs.FillMode = FillMode.WireFrame;
        graphicsDevice.RasterizerState = rs;

        basicEffect.World = Matrix.Identity;
        basicEffect.View = viewMatrix;
        basicEffect.Projection = projectionMatrix;
        basicEffect.VertexColorEnabled = true;

        basicEffect.DiffuseColor = new Vector3(1f, 1f, 1f);
        basicEffect.CurrentTechnique.Passes[0].Apply();

        foreach (PathSegment segment in Segments)
        {
            // foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            // {
            //     pass.Apply();
            //     graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, segment.Vertices, 0, segment.Vertices.Length, segment.Indices, 0, segment.Indices.Length / 3, VertexPositionColor.VertexDeclaration);
            // }

            //segment.DebugDraw(graphicsDevice, viewMatrix, projectionMatrix);
            for (int i = 0; i < segment.DebugPoints.Length - 1; i ++)
            {
                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{segment.DebugPoints[i], segment.DebugPoints[i + 1]}, 0, 1);
            }
        }

        if ((state != State.One) && meshGenerated)
        {
            //Console.WriteLine(OutlineSegment.DebugPoints.Length);
            for (int i = 0; i < OutlineSegment.DebugPoints.Length - 1; i++)
            {
                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{OutlineSegment.DebugPoints[i], OutlineSegment.DebugPoints[i + 1]}, 0, 1);
            }
        }

        basicEffect.DiffuseColor = new Vector3(0, 0, 1f);
        basicEffect.CurrentTechnique.Passes[0].Apply();

        foreach (PathSegmentConnector pathSegmentConnector in PathSegmentConnectors)
        {
            for (int i = 0; i < pathSegmentConnector.SegmentEntities.Count; i++)
            {
                //int segmentID = pathSegmentConnector.SegmentIDs[i];
                //int pathID = pathSegmentConnector.PointIDs[i];
                //PathSegment segment = Segments[segmentID];
                
                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, new[]{pathSegmentConnector.DebugPosition, pathSegmentConnector.DebugPoints[i]}, 0, 1);
            }
        }
    }
}
