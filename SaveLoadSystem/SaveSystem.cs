
using System;
using System.Collections.Generic;
using System.IO;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Microsoft.Xna.Framework;

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
          
     }

     public static void Save()
     {
          //what all do we need to save and load?
               //save the state of the entity manager
          //byte[] msgpackBytes = MessagePackSerializer.Serialize(EntityManager.Entities, options);
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

          // foreach (var entry in serializedComponents)
          // {
          //      Type t = Type.GetType(entry.typeName);
          //      object loaded = MessagePackSerializer.Deserialize(t,entry.data, options);
          //      // cast and register in ECS
          // }
     }
}