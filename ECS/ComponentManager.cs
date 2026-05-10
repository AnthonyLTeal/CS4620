using System;
using System.Collections.Generic;
using System.Reflection;
using CS4620IS.Components;

namespace CS4620IS;

public class ComponentManager
{
    public static readonly int _maxEntities = 16384;
    public static readonly int _defaultSize = 16384;
    public static readonly int _maxComponents = 64;
    
    private static bool reserved = false;
    public static int LastAddedEntity = 0;
    public static int EntityCount = 0;
    public static Stack<int> DeadEntities = new Stack<int>();
    public static int[] EntityGen = new int[_maxEntities];

    //public static int[] EntityGen = entityGen;
    //public static int EntityCount = entityCount;
    //public static Stack<int> DeadEntities = deadEntities;
    //public static int LastAddedEntity = lastAddedEntity;

    public static List<Array> ComponentRegistry = new List<Array>();
    public static Dictionary<Type, int> ComponentIDs = new Dictionary<Type, int>();
    public static List<int[]> ComponentOwners = new List<int[]>();
    public static int[] PoolCounts = new int[_maxComponents];
    public static int TotalComponents = 0;
    
    public static int[][] Entities = new int[_maxEntities][];

    public static void ResetComponentPools()
    {
        for (int componentId = 0; componentId < TotalComponents; componentId++)
        {
            Type elementType = ComponentRegistry[componentId].GetType().GetElementType()!;
            ComponentRegistry[componentId] = Array.CreateInstance(elementType, _defaultSize);
            ComponentOwners[componentId] = new int[_defaultSize];
            PoolCounts[componentId] = 0;
        }
    }

    public static void ResetKeepGlobal()
    {
        List<(object, int)> globalComponents = new List<(object, int)>();
        if (Entities[0] != null)
        {
            for (int i = 0; i < TotalComponents; i++)
            {
                int denseId = Entities[0][i];
                if (denseId == -1) continue;
                Array pool = ComponentRegistry[i];
                object value = pool.GetValue(denseId)!;
                globalComponents.Add((value, i));
            }
        }

        reserved = false;
        DeadEntities = new Stack<int>();
        LastAddedEntity = 0;
        EntityCount = 0;
        Entities = new int[_maxEntities][];
        Array.Clear(EntityGen, 0, EntityGen.Length);
        ResetComponentPools();
        ReserveZero();
        
        int entityIdCid = GetComponentID<EntityID>();
        foreach (var (component, id) in globalComponents)
        {
            if (id == entityIdCid) continue;
            AddComponent(0, component, id);
        }
    }
    
    public static int AddEntity()
    {
        if (DeadEntities.Count == 0)
        {
            LastAddedEntity = EntityCount;
            EntityCount += 1;
        } else {
            //TODO need to change DeadEntities to a stack/queue for faster operation
            LastAddedEntity = DeadEntities.Pop();
        }
        
        // IMPORTANT: Ensure the entity's component row is ready
        if (Entities[LastAddedEntity] == null) {
            Entities[LastAddedEntity] = new int[_maxComponents];
            Array.Fill(Entities[LastAddedEntity], -1);
        }
        
        AddComponent(LastAddedEntity, new EntityID() { ID = LastAddedEntity});
        return LastAddedEntity;
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

    // public static void DestroyEntity(int entity)
    // {
    //     for (int i = 0; i < Entities[entity].Length; i++)
    //     {
    //         int denseIndex =  Entities[entity][i];
    //         if (denseIndex != -1)
    //             RemoveComponent(entity, i);
    //     }
    //     
    //     deadEntities.Push(entity);
    //     entityGen[entity] += 1;
    // }
    
    //This one will check to make sure an entity isn't already dead, if it is, we can end up with duplicate entities from the dead list
    public static void DestroyEntity(int entity)
    {
        if (entity <= 0 || entity >= Entities.Length) 
            return;
        int[] row = Entities[entity];
        if (row == null)
            return;
        bool hadAnyComponent = false;
        for (int i = 0; i < row.Length; i++)
        {
            if (row[i] == -1) continue;
            hadAnyComponent = true;
            RemoveComponent(entity, i);
        }
        if (!hadAnyComponent)
            return;
        DeadEntities.Push(entity);
        EntityGen[entity] += 1;
    }

    public static void ReserveZero()
    {
        if (reserved)
            return;

        AddEntity();
        EntityGen[0] = 1;

        LastAddedEntity = 0;
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
    
    public static void AddComponent(int entity, object component, int componentId)
    {
        if (Entities[entity] == null) {
            Entities[entity] = new int[_maxComponents];
            Array.Fill(Entities[entity], -1);
        }
        
        if (component is null) return;
        Type expected = ComponentRegistry[componentId].GetType().GetElementType()!;
        if (!expected.IsInstanceOfType(component))
            throw new InvalidOperationException($"Component type mismatch: expected {expected}, got {component.GetType()}");
        
        ref int poolCount = ref PoolCounts[componentId];

        if (poolCount >= _defaultSize)
        {
            Console.WriteLine("Can't exceed the maximum number of components");
            return;
        }
        
        int denseId = poolCount;
        ComponentOwners[componentId][denseId] = entity;
        
        Array pool = ComponentRegistry[componentId];
        pool.SetValue(component, denseId);

        Entities[entity][componentId] = denseId;
        
        poolCount += 1;
    }
    
    public static void AddComponent(int entity, object component, Type t)
    {
        if (Entities[entity] == null) {
            Entities[entity] = new int[_maxComponents];
            Array.Fill(Entities[entity], -1);
        }
        
        if (component is null) return;
        int componentId = GetComponentID(t);
        Type expected = ComponentRegistry[componentId].GetType().GetElementType()!;
        if (!expected.IsInstanceOfType(component))
            throw new InvalidOperationException($"Component type mismatch: expected {expected}, got {component.GetType()}");
        
        ref int poolCount = ref PoolCounts[componentId];

        if (poolCount >= _defaultSize)
        {
            Console.WriteLine("Can't exceed the maximum number of components");
            return;
        }
        
        int denseId = poolCount;
        ComponentOwners[componentId][denseId] = entity;
        
        Array pool = ComponentRegistry[componentId];
        pool.SetValue(component, denseId);

        Entities[entity][componentId] = denseId;
        
        poolCount += 1;
    }

    public static void AddComponent<T>(int entity, T component)
    {
        if (Entities[entity] == null) {
            Entities[entity] = new int[_maxComponents];
            Array.Fill(Entities[entity], -1);
        }
        
        int componentId =  GetComponentID<T>();
        ref int poolCount = ref PoolCounts[componentId];

        if (poolCount >= _defaultSize)
        {
            Console.WriteLine("Can't exceed the maximum number of components");
            return;
        }
        
        int denseId = poolCount;
        ComponentOwners[componentId][denseId] = entity;
        
        T[] pool = (T[])ComponentRegistry[componentId];
        pool[denseId] = component;

        Entities[entity][componentId] = denseId;
        
        poolCount += 1;
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