
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
                    new Vector3Formatter()
               },
               new IFormatterResolver[]
               {
                    StandardResolver.Instance
               }
          );

          Options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
          
          //byte[] msgpackBytes = MessagePackSerializer.Serialize(myObject, options);
          //Vector3 myObject2 = MessagePackSerializer.Deserialize<Vector3>(msgpackBytes, options);
          
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

          byte[] msgpackBytes = MessagePackSerializer.Serialize(worldState, Options);
          File.WriteAllBytes("save.dat", msgpackBytes);
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
                         ComponentsData = MessagePackSerializer.Serialize(comp, Options),
                         TypeName = comp.GetType().AssemblyQualifiedName
                    };
                    savedComponents.Add(component);
               }
          }

          return savedComponents;
     }
}