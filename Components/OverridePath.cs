using System.IO;
using System.Runtime.CompilerServices;
using MessagePack;
using Microsoft.Xna.Framework;

namespace CS4620IS.Components;

[MessagePackObject(keyAsPropertyName: true)]
public struct OverridePath
{
    public Vector3 P1;
    public Vector3 P2;
    public Vector3 P3;
    public int CurrentPoint;
    public bool Overridden;

    public Vector3 Pop()
    {
        Vector3 p = P3;
        if (CurrentPoint == 0)
            p = P1;
        if (CurrentPoint == 1)
            p = P2;
        if (CurrentPoint == 2)
            p = P3;

        CurrentPoint += 1;
        
        return p;
    }

    public Vector3 Peek()
    {
        if (CurrentPoint == 0)
            return P1;
        if (CurrentPoint == 1)
            return P2;
        if (CurrentPoint == 2)
            return P3;
        
        return Vector3.Zero;
    }

    public static Vector3 NextPoint(ref OverridePath path)
    {
        path.CurrentPoint += 1;
        if (path.CurrentPoint == 1)
            return path.P2;
        if (path.CurrentPoint == 2)
            return path.P3;
        
        return Vector3.Zero;
    }

    public static Vector3 GetCurrentPoint(ref OverridePath path)
    {
        if (path.CurrentPoint == 0)
            return path.P1;
        if (path.CurrentPoint == 1)
            return path.P2;
        if (path.CurrentPoint == 2)
            return path.P3;
        
        return Vector3.Zero;
    }
}