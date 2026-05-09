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
    public int Count;
    public bool Overridden;

    public Vector3 Pop()
    {
        Count -= 1;

        if (Count == 2)
            return P1;
        if (Count == 1)
            return P2;
//        if (Count == 0)
        return P3;
    }

    public Vector3 Peek()
    {
        if (Count == 3)
            return P1;
        if (Count == 2)
            return P2;
        //if (Count == 2)
        //    return P3;

        return P3;
    }

// public static Vector3 NextPoint(ref OverridePath path)
    // {
    //     path.CurrentPoint += 1;
    //     if (path.CurrentPoint == 1)
    //         return path.P2;
    //     if (path.CurrentPoint == 2)
    //         return path.P3;
    //     
    //     return Vector3.Zero;
    // }
    //
    // public static Vector3 GetCurrentPoint(ref OverridePath path)
    // {
    //     if (path.CurrentPoint == 0)
    //         return path.P1;
    //     if (path.CurrentPoint == 1)
    //         return path.P2;
    //     if (path.CurrentPoint == 2)
    //         return path.P3;
    //     
    //     return Vector3.Zero;
    // }
}