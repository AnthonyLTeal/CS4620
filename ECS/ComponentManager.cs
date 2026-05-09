using System;
using System.Collections.Generic;
using System.Reflection;
using CS4620IS.Components;

namespace CS4620IS;

public class ComponentManager
{
    private static readonly int _maxEntities = 16384;
    private static readonly int _defaultSize = 16384;
    private static readonly int _maxComponents = 64;
    
    private static bool reserved = false;
    private static int lastAddedEntity = 0;
    private static int entityCount = 0;
    private static Stack<int> deadEntities = new Stack<int>();
    private static int[] entityGen = new int[_maxEntities];
    
    public static List<Array> ComponentRegistry = new List<Array>();
    public static Dictionary<Type, int> ComponentIDs = new Dictionary<Type, int>();
    public static List<int[]> ComponentOwners = new List<int[]>();
    public static int[] PoolCounts = new int[_maxComponents];
    public static int TotalComponents = 0;
    
    public static int[][] Entities = new int[_maxEntities][];
    
    public static int AddEntity()
    {
        if (deadEntities.Count == 0)
        {
            lastAddedEntity = entityCount;
            entityCount += 1;
        } else {
            //TODO need to change DeadEntities to a stack/queue for faster operation
            lastAddedEntity = deadEntities.Pop();
        }
        
        // IMPORTANT: Ensure the entity's component row is ready
        if (Entities[lastAddedEntity] == null) {
            Entities[lastAddedEntity] = new int[_maxComponents];
            Array.Fill(Entities[lastAddedEntity], -1);
        }
        
        AddComponent(lastAddedEntity, new EntityID() { ID = lastAddedEntity});
        return lastAddedEntity;
    }

    public static int GetCount<T>()
    {
        return PoolCounts[GetComponentID<T>()];
    }

    public static int[] GetOwners<T>()
    {
        return ComponentOwners[GetComponentID<T>()];
    }

    public static T GetGlobalComponent<T>()
    {
        return GetComponent<T>(0);
    }

    public static void DestroyEntity(int entity)
    {
        for (int i = 0; i < Entities[entity].Length; i++)
        {
            int denseIndex =  Entities[entity][i];
            if (denseIndex != -1)
                RemoveComponent(entity, i);
        }
        
        deadEntities.Push(entity);
        entityGen[entity] += 1;
    }

    public static void ReserveZero()
    {
        if (reserved)
            return;

        AddEntity();
        entityGen[0] = 1;

        lastAddedEntity = 0;
        reserved = true;
    }
    
    public static ref T GetComponent<T>(int entity)
    {
        int componentId = GetComponentID<T>();
        int denseId = Entities[entity][componentId];
        
        System.Diagnostics.Debug.Assert(denseId != -1, $"Entity {entity} does not have component {typeof(T).Name}");
        // if (denseId == -1)
        // {
        //     return default;
        // }
        
        T[] components = (T[])ComponentRegistry[componentId];
        return ref components[denseId];
    }

    public static void RegisterComponent<T>()
    {
        ComponentRegistry.Add(new T[_defaultSize]);
        ComponentIDs.Add(typeof(T), TotalComponents);
        ComponentOwners.Add(new int[_defaultSize]);
        TotalComponents += 1;
    }

    public static void RegisterComponent(Type t)
    {
        ComponentRegistry.Add(Array.CreateInstance(t, _defaultSize));
        ComponentIDs.Add(t, TotalComponents);
        ComponentOwners.Add(new int[_defaultSize]);
        TotalComponents += 1;
    }

    public static void AddComponentToGlobalEntity<T>(T component)
    {
        AddComponent(0, component);
    }

    public static void AddComponent<T>(int entity, T component)
    {
        int componentId =  GetComponentID<T>();
        ref int poolCount = ref PoolCounts[componentId];

        if (poolCount >= _defaultSize)
        {
            Console.WriteLine("Can't exceed the maximum number of components");
            return;
        }
        
        int denseId = PoolCounts[componentId];
        ComponentOwners[componentId][denseId] = entity;
        
        T[] pool = (T[])ComponentRegistry[componentId];
        pool[denseId] = component;

        Entities[entity][componentId] = denseId;
        
        PoolCounts[componentId] += 1;
    }

    public static void RemoveComponent(int entity, int componentId)
    {
        int denseId = Entities[entity][componentId];
        int count = PoolCounts[componentId];
        
        Array pool = ComponentRegistry[componentId];
        Array.Copy(pool, count - 1, pool, denseId, 1);
        
        int lastEntity = ComponentOwners[componentId][count - 1];
        Entities[lastEntity][componentId] = denseId;
        ComponentOwners[componentId][denseId] = lastEntity;
    
        Entities[entity][componentId] = -1;
        PoolCounts[componentId] -= 1;
    }

    public static void RemoveComponent<T>(int entity)
    {
        RemoveComponent(entity, GetComponentID<T>());
        // int componentId =  GetComponentID<T>();
        // int denseId = Entities[entity][componentId];
        // int count = PoolCounts[componentId];
        // T[] pool = (T[])ComponentRegistry[componentId];
        //
        // pool[denseId] = pool[count - 1];
        //
        // int lastEntity = ComponentOwners[componentId][count - 1];
        //
        // Entities[lastEntity][componentId] = denseId;
        // ComponentOwners[componentId][denseId] = lastEntity;
        //
        // Entities[entity][componentId] = -1;
        //
        // PoolCounts[componentId] -= 1;
    }

    public static T[] GetComponents<T>()
    {
        return (T[])ComponentRegistry[GetComponentID<T>()];
    }

    public static int GetComponentID(Type t)
    {
        return ComponentIDs[t];
    }

    public static int GetComponentID<T>()
    {
        return ComponentIDs[typeof(T)];
    }

    public static void LoadComponentsFromNamespace(string @namespace)
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (Type t in types)
        {
            if (t.Namespace == @namespace && !t.IsAbstract && !t.IsGenericTypeDefinition)
            {
                RegisterComponent(t);
            }
        }
    }
}