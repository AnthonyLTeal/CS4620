
using System;
using System.Collections.Generic;
using System.IO;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.Xna.Framework;
using PlanetaryExpansion;

namespace CS4620IS;

public class SaveSystem
{
     private static MessagePackSerializerOptions options;
     
     public static void RegisterFormatters()
     {
          var resolver = CompositeResolver.Create(
               new IMessagePackFormatter[]
               {
                    new Vector3Formatter()
               },
               new IFormatterResolver[]
               {
                    StandardResolver.Instance
               }
          );

          options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
          
          //byte[] msgpackBytes = MessagePackSerializer.Serialize(myObject, options);
          //Vector3 myObject2 = MessagePackSerializer.Deserialize<Vector3>(msgpackBytes, options);
          
     }

     public static void Load()
     {
          //first load EntityManager state
          //next load ComponentManager state
          //next load Components
          //Next load roadmesh
          //load PathSegmentConnectors
          //insert segments into Segments list (these should be inserted in order as they appear in EntityManager. I'm guessing the order is retained in the component manager list but will need to double check)
          //either load Octree or just rebuild (probably rebuild)
          //recreate ribbon mesh

     }

     public static void Save()
     {
          WorldState worldState = new WorldState();
          worldState.Entities = EntityManager.Entities;
          worldState.EntityComponents = SerializeEntityComponents();
          worldState.Reserved = EntityManager.Reserved;
          worldState.DeadEntities = EntityManager.DeadEntities;
          worldState.LastAddedEntity = EntityManager.LastAddedEntity;
          
          //This probably doesn't need to be saved and should be rebuilt correctly just by using AddComponentToEntity 
          //worldState.ComponentRegistery = ComponentManager.ComponentRegistry; 
          
          //these 2 we might just want to rebuild and then when reloading we just have to respect the order or invalidate saves with an invalid order
          //worldState.ComponentIDs = ComponentManager.ComponentIDs;
          //worldState.TotalComponents = ComponentManager.TotalComponents;

          worldState.PathSegmentConnectors = EntityManager.GetGlobalComponent<RoadMesh>().PathSegmentConnectors;

          byte[] msgpackBytes = MessagePackSerializer.Serialize(worldState, options);
          File.WriteAllBytes("save.dat", msgpackBytes);
     }

     public static void LoadEntityComponents()
     {
          byte[] bytes = File.ReadAllBytes("save.dat");
          WorldState worldState = MessagePackSerializer.Deserialize<WorldState>(bytes, options);
          // foreach (var entry in serializedComponents)
          // {
          //      Type t = Type.GetType(entry.typeName);
          //      object loaded = MessagePackSerializer.Deserialize(t,entry.data, options);
          //      // cast and register in ECS
          // }
     }

     public static List<SavedComponent> SerializeEntityComponents()
     {
          List<SavedComponent> savedComponents = new List<SavedComponent>();
          List<object[]> entityComponents = EntityManager.EntityComponents;
          
          // var serializedComponents = new List<(string typeName, byte[] data)>();

          for (int i = 1; i < entityComponents.Count; i ++)
          {
               foreach (var comp in entityComponents[i])
               {
                    if (comp == null)
                         continue;
                    
                    SavedComponent component = new SavedComponent()
                    {
                         EntityID = i,
                         ComponentsData = MessagePackSerializer.Serialize(comp, options),
                         TypeName = comp.GetType().AssemblyQualifiedName
                    };
                    savedComponents.Add(component);
               }
          }

          return savedComponents;
     }
}