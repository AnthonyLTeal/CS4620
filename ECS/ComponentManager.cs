using System;
using System.Collections.Generic;
using System.Reflection;

namespace CS4620IS;

public class ComponentManager
{
    public static Dictionary<Type, List<int>> ComponentRegistry = new Dictionary<Type, List<int>>();
    public static Dictionary<Type, int> ComponentIDs = new Dictionary<Type, int>();
    public static int TotalComponents = 0;
    
    public static T GetEntityComponent<T>(int entity)
    {
        int componentID = GetComponentID<T>();
        return (T)EntityManager.EntityComponents[entity][componentID];
    }

    public static void RegisterComponent<T>()
    {
        ComponentRegistry.Add(typeof(T), new List<int>());
        ComponentIDs.Add(typeof(T), TotalComponents);
        TotalComponents += 1;
    }

    public static void RegisterComponent(Type t)
    {
        ComponentRegistry.Add(t, new List<int>());
        ComponentIDs.Add(t, TotalComponents);
        TotalComponents += 1;
    }

    public static List<int> GetComponent<T>()
    {
        return ComponentRegistry[typeof(T)];
    }

    public static int GetComponentID(Type t)
    {
        return ComponentIDs[t];
    }

    public static int GetComponentID<T>()
    {
        return ComponentIDs[typeof(T)];
    }

    public static void LoadComponentsFromNamespace(string _namespace)
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (Type t in types)
        {
            if (t.Namespace==_namespace)
            {
                RegisterComponent(t);
            }
        }
    }
}