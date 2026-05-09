using Microsoft.Xna.Framework;

namespace CS4620IS.Components;

public struct VisualComponent
{
    public Color Color;
    public float Scale = 0.25f; 
    
    public VisualComponent(Color color, float scale)
    {
        Color = color;
        Scale = scale;
    }
}