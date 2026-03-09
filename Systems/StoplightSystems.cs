using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Timers;
using CS4620IS.Collision;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlanetaryExpansion;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace CS4620IS;

public class StoplightSystems
{
    
    //public static Dictionary<int, bool> lights = new Dictionary<int, bool>();

     //test this function 
    public static List<(int, int)> GenerateStoplights(int id)
    {
        List<(int, int)> stoplightConnections = new List<(int, int)>();
        List<int> segmentidsWithLights = new List<int>();
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegmentConnector connector = roadMesh.PathSegmentConnectors[id];
        connector.StoplightConnections = stoplightConnections;
        
            
        for (int i = 0; i < connector.SegmentEntities.Count; i++)
        {
            PathSegment segment1 = ComponentManager.GetEntityComponent<PathSegment>(connector.SegmentEntities[i]);
            if (connector.SegmentEntities.Count <= 2) {continue;}
            //if (stoplightConnections.Any(t => t.Item1 == segment1.ID)) {continue;}
            float smallestDotValue = -1;
            int sharedSegmentID = -1;
            if (segmentidsWithLights.Contains(segment1.ID)) {continue;}
            Vector3 point1;
            point1 = segment1.Path[0];
            if (segment1.EndConnector == connector.ID) {point1 = segment1.Path[^1];}
            Vector3 maindirection = Vector3.Normalize(point1 - connector.Position);
            
            for (int j = 0; j < connector.SegmentEntities.Count; j++)
            {
                PathSegment segment2 = ComponentManager.GetEntityComponent<PathSegment>(connector.SegmentEntities[j]);
                //if (stoplightConnections.Any(t => t.Item2 == segment2.ID)) {continue;}
                if (i == j) {continue;}
                if (segmentidsWithLights.Contains(segment2.ID)) {continue;}
                Vector3 point2;
                point2 = segment2.Path[0];
                if (segment2.EndConnector == connector.ID) {point2 = segment2.Path[^1];}
                Vector3 nextDirection = Vector3.Normalize(point2 - connector.Position);
                float dotProduct = Vector3.Dot(maindirection, nextDirection);
                if (dotProduct > smallestDotValue)
                {
                    smallestDotValue = dotProduct;
                    sharedSegmentID = segment2.ID;
                }
            }
            if (sharedSegmentID != -1) {segmentidsWithLights.Add(sharedSegmentID);}
            segmentidsWithLights.Add(segment1.ID);
            //segmentidsWithLights.Add(sharedSegmentID);
            stoplightConnections.Add((segment1.ID, sharedSegmentID));
            
        }
        return stoplightConnections;
    }

    public static void ChangeRedGreen(GameTime gametime)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        List<PathSegmentConnector> connectors = roadMesh.PathSegmentConnectors;
        for (int i = 0; i < connectors.Count; i++)
        {
            if (connectors[i].LightTime <= connectors[i].LightTimer)
            {
                connectors[i].CurrentLightGreen += 1;
                if (connectors[i].CurrentLightGreen >= connectors[i].StoplightConnections.Count)
                {
                    connectors[i].CurrentLightGreen = 0;
                } 
                connectors[i].LightTimer = 0;
            }
            

            if (connectors[i].LightTime > connectors[i].LightTimer)
            {
                connectors[i].LightTimer += gametime.ElapsedGameTime.TotalMilliseconds;
            }
        }
    }
    public static bool WaitOnStopLight(int carEntity)
    {
        Car car = ComponentManager.GetEntityComponent<Car>(carEntity);
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        if (car.InIntersection)
        {
            return false;
        }
        
        if (car.OnConnector == -1) {return false;}
        
        PathSegmentConnector connector = roadMesh.PathSegmentConnectors[car.OnConnector];
        

        if (connector.SegmentEntities.Count <= 2) {return false;}
        
        //don't make it wait if it's connector is green
        int segmentOne;
        int segmentTwo;
        (segmentOne, segmentTwo) = connector.StoplightConnections[connector.CurrentLightGreen];
        if (segmentOne == car.PreviousSegment || segmentTwo == car.PreviousSegment) 
        {
            if (connector.LightTime - connector.LightTimer > connector.YellowTimer)
            {
                car.InIntersection = true;
                return false;
            }
        }
        return true;
    }
}