using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PlanetaryExpansion.Components;

public class ModelInfo
{
    public string ModelKey;
    public string TextureKey;
    public string EffectKey;
    public int InstancedAssetParentEntity;
    public bool IsInstanced;
}



public class DrawableAsset
{
    public Matrix Position;
    public Matrix Rotation;
    public float Scale;
}

public class InstancedAsset
{
    public Matrix Position;
    public Matrix Rotation;
    public float Scale;
    public int InstancedAssetParentEntity;
    public int PositionIndex;
}

public class InstancedAssetParent
{
    public Matrix[] Positions;
    public int[] PositionEntity;
    public VertexDeclaration InstanceVertexDeclaration;
    public DynamicVertexBuffer InstanceVertexBuffer;
    public int Count = 0;
}

public struct Name
{
    public string Value;
}

public class KeyLight
{
    public Vector3 Direction;
    public Matrix View;
    public Matrix World;
    public Matrix Projection;
    public float Rotation;
    public float RotationSpeed;
    public float AxialTilt;
}

public class Shadow
{
    public Texture2D Map;
    public float Size;
    public float Bias;
}
