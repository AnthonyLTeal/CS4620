using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CS4620IS.Components;

namespace CS4620IS;

public class EntityManager
{
    private static readonly int _maxEntities = 10000;
    private static int entityCount = 0;
    public static int[][] Entities = new int[_maxEntities][]; //stores indices that tell where the entities components are in the corresponding component list
    public static bool Reserved = false;
    public static List<int> DeadEntities = new List<int>();
    private static int lastAddedEntity = 0;
    private static List<int> entityGen = new List<int>();

    public static int AddEntity()
    {
        if (DeadEntities.Count == 0)
        {
            lastAddedEntity = entityCount;
            entityCount += 1;
            entityGen.Add(0);
        } else {
            //TODO need to change DeadEntities to a stack/queue for faster operation
            lastAddedEntity = entityGen[DeadEntities[0]];
            DeadEntities.RemoveAt(0);
        }
        ComponentManager.AddComponent(lastAddedEntity, new EntityID() { ID = lastAddedEntity});
        return lastAddedEntity;
    }

    public static int NextEntity()
    {
        if (DeadEntities.Count == 0)
        {
            return entityCount;
        }

        return DeadEntities[0];
    }

    /// <summary>
    /// Adds a component to the global entity
    /// This entity is at index 0 and can be manually added to as well
    /// </summary>
    /// <param name="component">Component to add to global entity</param>
    /// <typeparam name="T">The type of component being added</typeparam>
    // public static void AddComponentToGlobalEntity<T>(T component)
    // {
    //     AddComponentToEntity<T>(0, component);
    // }

    // public static void AddComponentToEntity<T>(int entity, T component)
    // {
    //     ReserveZero();
    //     List<int> cRegister = ComponentManager.ComponentRegistry[ComponentManager.ComponentIDs[typeof(T)]];
    //     cRegister.Add(entity);
    //     EntityComponents[entity][ComponentManager.GetComponentID<T>()] = component;
    //     Entities[entity][ComponentManager.GetComponentID<T>()] = cRegister.Count;
    // }
    //
    // public static void AddComponentToEntity(int entity, object component, Type t)
    // {
    //     ReserveZero();
    //     List<int> cRegister = ComponentManager.ComponentRegistry[ComponentManager.ComponentIDs[t]];
    //     cRegister.Add(entity);
    //     EntityComponents[entity][ComponentManager.GetComponentID(t)] = component;
    //     Entities[entity][ComponentManager.GetComponentID(t)] = cRegister.Count;
    // }
    
    // private static void ReserveZero()
    // {
    //     if (Reserved) { return; }
    //
    //     AddEntity();
    //     entityGen.Add(0);
    //     
    //     lastAddedEntity = Entities.Count - 1;
    //     Reserved = true;
    // }

    public static void RemoveEntity(int index)
    {
        //Console.WriteLine("Removing Entity: " + index);
        int[] entity = Entities[index];
        for (int i = 0; i < ComponentManager.TotalComponents; i++)
        {
            //TODO there is something weird happening here I think?
            //this condition checks if an object has been created, it will evaluate to false if an object exists at that index
            if (entity[i] > 0)
            {
                int componentIndex = i;
                List<int> componentEntities = ComponentManager.ComponentRegistry[componentIndex];
                int lastEntity = componentEntities.Last();
                Entities[lastEntity][componentIndex] = entity[componentIndex];
                EntityComponents[index][componentIndex] = null;
                componentEntities[entity[i]-1] = lastEntity;
                componentEntities.RemoveAt(componentEntities.Count - 1);
            }
        }
        DeadEntities.Add(index);
        entityGen[index] += 1;
        //Console.WriteLine("Total Free Entities: " + DeadEntities.Count);
    }
    
    public static T GetGlobalComponent<T>()
    {
        int componentID = ComponentManager.GetComponentID<T>();
        T globalComponent = ComponentManager.GetComponent<T>(0);
        return globalComponent;
    }

    public static int LastAddedEntity
    {
        get { return lastAddedEntity; }
    }

    public static List<int> EntityGen
    {
        get { return entityGen; }
    }

    public static bool IsAlive(EntityRef entityRef)
    {
        return entityGen[entityRef.ID] == entityRef.Gen;
    }

    public static void SetLastEntityFromLoad(int entity)
    {
        lastAddedEntity = entity;
    }
    
    public static void SetLastEntityGenFromLoad(List<int> entityGen)
    {
        EntityManager.entityGen = entityGen;
    }
}