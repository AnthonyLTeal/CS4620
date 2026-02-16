using System;
using System.Collections.Generic;
using System.IO;
using MessagePack;
using PlanetaryExpansion;

namespace CS4620IS;

public class LoadSystem
{
    public static void Load()
    {
        byte[] bytes = File.ReadAllBytes("save.dat");
        //first load EntityManager state
        //next load ComponentManager state
        //next load Components
        //Next load roadmesh
        //load PathSegmentConnectors
        //insert segments into Segments list (these should be inserted in order as they appear in EntityManager. I'm guessing the order is retained in the component manager list but will need to double check)
        //either load Octree or just rebuild (probably rebuild)
        //recreate ribbon mesh
        
        WorldState worldState = MessagePackSerializer.Deserialize<WorldState>(bytes);
        EntityManager.Entities = worldState.Entities;

        List<SavedComponent> savedComponents = worldState.EntityComponents;
        LoadEntityComponents(savedComponents, EntityManager.Entities.Count);
        
        EntityManager.Reserved = worldState.Reserved;
        EntityManager.DeadEntities = worldState.DeadEntities;
        EntityManager.SetLastEntityFromLoad(worldState.LastAddedEntity);
        EntityManager.GetGlobalComponent<RoadMesh>().PathSegmentConnectors = worldState.PathSegmentConnectors;
        
          
        //This probably doesn't need to be saved and should be rebuilt correctly just by using AddComponentToEntity 
        //worldState.ComponentRegistery = ComponentManager.ComponentRegistry; 
          
        //these 2 we might just want to rebuild and then when reloading we just have to respect the order or invalidate saves with an invalid order
        //worldState.ComponentIDs = ComponentManager.ComponentIDs;
        //worldState.TotalComponents = ComponentManager.TotalComponents;
    }
    
    public static void LoadEntityComponents(List<SavedComponent> savedComponents, int entityCount)
    {
        for (int i = 0; i < entityCount - 1; i++) //we subtract one for the global entity which will be created automatically by the EntityManager
        {
            EntityManager.Entities.Add(new int[ComponentManager.TotalComponents]);
            EntityManager.EntityComponents.Add(new object[ComponentManager.TotalComponents]);
        }
        //WorldState worldState = MessagePackSerializer.Deserialize<WorldState>(bytes, SaveSystem.Options);
        foreach (SavedComponent entry in savedComponents)
        {
             Type t = Type.GetType(entry.TypeName);
             object loaded = MessagePackSerializer.Deserialize(t,entry.ComponentsData, SaveSystem.Options);
             
             EntityManager.AddComponentToEntity(entry.EntityID, loaded, t);
        }
    }
}