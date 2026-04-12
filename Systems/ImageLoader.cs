using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace CS4620IS;

public class ImageLoader
{
    public static Texture2D LoadImage(GraphicsDevice graphicsDevice, string path)
    {
        using (var stream = new FileStream(path, FileMode.Open))
        {
            return Texture2D.FromStream(graphicsDevice, stream);
        }
    }
}