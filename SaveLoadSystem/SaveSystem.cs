
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
     public static MessagePackSerializerOptions Options;
     
     public static void RegisterFormatters()
     {
          var resolver = CompositeResolver.Create(
               new IMessagePackFormatter[]
               {
                    new Vector3Formatter(),
                    new ColorFormatter(),
                    new VertexPositionColorFormatter()
               },
               new IFormatterResolver[]
               {
                    StandardResolver.Instance
               }
          );

          Options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
     }

     public static void Save(String path)
     {
          WorldState worldState = new WorldState();
          int total = ComponentManager.Entities.Length;
          worldState.Entities = new List<int[]>(total);
          worldState.Entities.Add(null); // slot 0 reserved/global
          for (int id = 1; id < total; id++)
          {
               int[] row = ComponentManager.Entities[id];
               worldState.Entities.Add(row == null ? null : (int[])row.Clone()); // deep copy row
          }
          
          worldState.EntityComponents = SerializeEntityComponents();
          worldState.DeadEntities = ComponentManager.DeadEntities;
          worldState.LastAddedEntity = ComponentManager.LastAddedEntity;
          worldState.EntityGen = ComponentManager.EntityGen;
          worldState.EntityCount = ComponentManager.EntityCount;
          
          //This probably doesn't need to be saved and should be rebuilt correctly just by using AddComponentToEntity 
          //worldState.ComponentRegistery = ComponentManager.ComponentRegistry; 
          
          //these 2 we might just want to rebuild and then when reloading we just have to respect the order or invalidate saves with an invalid order
          //worldState.ComponentIDs = ComponentManager.ComponentIDs;
          //worldState.TotalComponents = ComponentManager.TotalComponents;
          
          worldState.PathSegmentConnectors = ComponentManager.GetGlobalComponent<RoadMesh>().PathSegmentConnectors;
          
          byte[] msgpackBytes = MessagePackSerializer.Serialize(worldState, Options);
          File.WriteAllBytes(path, msgpackBytes);
     }

     public static List<SavedComponent> SerializeEntityComponents()
     {
          List<SavedComponent> savedComponents = new List<SavedComponent>();

          List<Array> componentRegistery = ComponentManager.ComponentRegistry;
          
          for (int componentID = 0; componentID < componentRegistery.Count; componentID++)
          {
               Array components = componentRegistery[componentID];
               int poolCount = ComponentManager.PoolCounts[componentID];
               int[] owners = ComponentManager.ComponentOwners[componentID];
               
               for (int i = 0; i < poolCount; i++)
               {
                    int entity = owners[i];
                    if (entity == 0) //don't save global entity components, we will want to recreate these manually
                         continue;
                    
                    object component = components.GetValue(i);
                    
                    SavedComponent savedComponent = new SavedComponent()
                    {
                         EntityID = entity,
                         ComponentsData = MessagePackSerializer.Serialize(component, Options),
                         TypeName = component.GetType().AssemblyQualifiedName
                    };
          
                    savedComponents.Add(savedComponent);
               }
          }

          return savedComponents;
     }

     public static string[] ListSaveFiles()
     {
          string currentDir = Directory.GetCurrentDirectory();
          string targetDir = Path.Combine(currentDir, "saves");

          if (!Directory.Exists(targetDir))
          {
               Directory.CreateDirectory(targetDir);
          }
          
          return Directory.GetFiles(targetDir);
     }
}