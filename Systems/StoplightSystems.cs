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
    public static void GenerateStoplights(int id)
    {
        PriorityQueue<(int, int), float> potentialConnections = new PriorityQueue<(int, int), float>();
        List<(int, int)> stoplightConnections = new List<(int, int)>();
        List<int> segments = new List<int>();
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        PathSegmentConnector connector = roadMesh.PathSegmentConnectors[id];
        connector.StoplightConnections = stoplightConnections;

        if (connector.SegmentEntities.Count < 3)
            return;
            
        for (int i = 0; i < connector.SegmentEntities.Count; i++)
        {
            int segmentOneEntity = connector.SegmentEntities[i];
            PathSegment segmentOne = ComponentManager.GetEntityComponent<PathSegment>(connector.SegmentEntities[i]);
            segments.Add(segmentOneEntity);
            if (connector.SegmentEntities.Count <= 2)
                continue;

            Vector3 p1 = segmentOne.Path[0];
            if (segmentOne.EndConnector == connector.ID) 
                p1 = segmentOne.Path[^1];
            Vector3 mainDirection = Vector3.Normalize(p1 - connector.Position);
            
            for (int j = i + 1; j < connector.SegmentEntities.Count; j++)
            {
                int segmentTwoEntity = connector.SegmentEntities[j];
                PathSegment segmentTwo = ComponentManager.GetEntityComponent<PathSegment>(connector.SegmentEntities[j]);
                
                Vector3 p2 = segmentTwo.Path[0];
                if (segmentTwo.EndConnector == connector.ID) 
                    p2 = segmentTwo.Path[^1];
                Vector3 nextDirection = Vector3.Normalize(p2 - connector.Position);
                float dotProduct = Vector3.Dot(mainDirection, nextDirection);
                potentialConnections.Enqueue((segmentOneEntity, segmentTwoEntity), dotProduct);
            }
            
        }

        while (segments.Count > 1)
        {
            int segmentOneEntity;
            int segmentTwoEntity;
            
            (segmentOneEntity, segmentTwoEntity) = potentialConnections.Dequeue();

            if (segments.Contains(segmentOneEntity) && segments.Contains(segmentTwoEntity))
            {
                segments.Remove(segmentOneEntity);
                segments.Remove(segmentTwoEntity);
                stoplightConnections.Add((segmentOneEntity, segmentTwoEntity));
            }
        }
        
        if (segments.Count == 1)
        {
            stoplightConnections.Add((-1, segments[0]));
        }

        int counter = 0;
        foreach (var light in stoplightConnections)
        {
            counter++;
            (int x, int y) = light;
            Console.WriteLine($"Stop Light: 0: {x}, 1: {y}");
        }
        
        //Console.WriteLine("Stop Light Count:  " + stoplightConnections.Count + "\n");
    }

    public static void ChangeRedGreen(float virtualDT)
    {
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        List<PathSegmentConnector> connectors = roadMesh.PathSegmentConnectors;
        for (int i = 0; i < connectors.Count; i++)
        {
            //Console.WriteLine("Light TIME: " +  connectors[i].LightTime);
            if (connectors[i].SegmentEntities.Count < 3)
                continue;

            if (connectors[i].LightTimer > connectors[i].LightTime)
            {
                connectors[i].CurrentLightGreen += 1;
                if (connectors[i].CurrentLightGreen >= connectors[i].StoplightConnections.Count)
                {
                    connectors[i].CurrentLightGreen = 0;
                } 
                connectors[i].LightTimer = 0;
            }
            else
            {
                connectors[i].LightTimer += virtualDT * 1000;
            }
        }
    }
    
    public static bool WaitOnStopLight(int carEntity)
    {
        Car car = ComponentManager.GetEntityComponent<Car>(carEntity);
        RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
        
        if (car.OnConnector == -1) {return false;}
        
        PathSegmentConnector connector = roadMesh.PathSegmentConnectors[car.OnConnector];
        
        if (connector.SegmentEntities.Count <= 2) {return false;}

        if (car.InIntersection)
            return false;

        (int segmentOne, int segmentTwo) = connector.StoplightConnections[connector.CurrentLightGreen];
        
        // Console.WriteLine("Segment One: " + segmentOne);
        // Console.WriteLine("Segment Two: " + segmentTwo);
        //
        // Console.WriteLine("Car previous Segment: " + car.PreviousSegment);
        //
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