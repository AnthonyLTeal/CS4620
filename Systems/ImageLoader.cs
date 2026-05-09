using System;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CS4620IS;

public class ImageLoader
{
    public static Texture2D LoadImage(GraphicsDevice graphicsDevice, string path, ContentManager content)
    {
        string _path = Path.Combine(content.RootDirectory, path);
        using (var stream = new FileStream(_path, FileMode.Open))
        {
            return Texture2D.FromStream(graphicsDevice, stream);
        }
    }

    // public static Texture2D LoadImage(GraphicsDevice graphicsDevice, string path)
    // {
    //     string _path = Path.Combine(content.RootDirectory, path);
    //     using (var stream = new FileStream(_path, FileMode.Open))
    //     {
    //         return Texture2D.FromStream(graphicsDevice, stream);
    //     }
    // }
}