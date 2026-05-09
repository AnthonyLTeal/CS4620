using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CS4620IS.Components;
using MessagePack;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlanetaryExpansion;

namespace CS4620IS;

public class LoadSystem
{
    public static void Load(String path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        //first load EntityManager state
        //next load ComponentManager state
        //next load Components
        //Next load roadmesh
        //load PathSegmentConnectors
        //insert segments into Segments list (these should be inserted in order as they appear in EntityManager. I'm guessing the order is retained in the component manager list but will need to double check)
        //either load Octree or just rebuild (probably rebuild)
        //recreate ribbon mesh

        ComponentManager.ResetKeepGlobal();
        
        WorldState worldState = MessagePackSerializer.Deserialize<WorldState>(bytes, SaveSystem.Options);

        List<SavedComponent> savedComponents = worldState.EntityComponents;
        LoadEntityComponents(savedComponents);
        
        ComponentManager.LastAddedEntity = worldState.LastAddedEntity;
        ComponentManager.DeadEntities = worldState.DeadEntities;
        ComponentManager.EntityGen = worldState.EntityGen;
        ComponentManager.EntityCount = worldState.EntityCount;
        
        RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
        roadMesh.DestroyAll();
        
        bool previousRoadMeshState = roadMesh.CreateEnabled;
        
        roadMesh.CreateEnabled = true;
        
        PathSegment[] segments = ComponentManager.GetComponents<PathSegment>();
        int segmentCount = ComponentManager.GetCount<PathSegment>();
        
        roadMesh.PathSegmentConnectors = worldState.PathSegmentConnectors;

        for (int i = 0; i < segmentCount; i++)
        {
            PathSegment segment = segments[i];
            roadMesh.GenerateRibbonMesh(segment);
            segment.DebugPoints = PathSegmentSystems.GenerateRoadOutline(segment.Path, true);
            roadMesh.InsertSegmentPathPointsIntoOctree(segment);
        }
        
        foreach (PathSegmentConnector connector in roadMesh.PathSegmentConnectors)
        {
            connector.DebugPosition = new VertexPositionColor(connector.Position, Color.Blue);
            roadMesh.InsertSegmentConnectorIntoOctree(connector);
            StoplightSystems.GenerateStoplights(connector.ID);
        }
        
        roadMesh.RebuildMesh();
        //this is the problem right here I am talking about gemini
        roadMesh.CreateEnabled = previousRoadMeshState;
        
        //This probably doesn't need to be saved and should be rebuilt correctly just by using AddComponentToEntity 
        //worldState.ComponentRegistery = ComponentManager.ComponentRegistry; 
        
        //these 2 we might just want to rebuild and then when reloading we just have to respect the order or invalidate saves with an invalid order
        //worldState.ComponentIDs = ComponentManager.ComponentIDs;
        //worldState.TotalComponents = ComponentManager.TotalComponents;
    }
    
    public static void LoadEntityComponents(List<SavedComponent> savedComponents)
    {
        // for (int i = 0; i < entityCount - 1; i++) //we subtract one for the global entity which will be created automatically by the EntityManager
        // {
        //     // EntityManager.EntityComponents.Add(new object[ComponentManager.TotalComponents]);
        // }
        
        foreach (SavedComponent entry in savedComponents)
        {
             Type t = Type.GetType(entry.TypeName);
             if (t == null)
                 continue;
             
             object loaded = MessagePackSerializer.Deserialize(t,entry.ComponentsData, SaveSystem.Options);
             
             ComponentManager.AddComponent(entry.EntityID, loaded, t);
        }
    }
}