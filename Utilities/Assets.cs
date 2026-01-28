using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;

namespace CS4620IS
{
	public static class Assets
	{
		public static Dictionary<string, Model> Models;
		public static Dictionary<string, Texture2D> Textures;
		public static Dictionary<string, SpriteFont> Fonts;
		public static Dictionary<string, Effect> Effects;
		public static ContentManager Content;

		public static void Load(ContentManager content)
		{
			Content = content;
			Textures = LoadAsset<Texture2D>(Content, "Textures");
			Models = LoadAsset<Model>(Content, "Models", true);
			Effects = LoadAsset<Effect>(Content, "Shaders", true);
		}

		public static Texture2D GetTexture(string path)
		{
			Texture2D texture;
			bool textureExists = Textures.TryGetValue(path, out texture);
			
			if (!textureExists)
			{
				//Could include a fallback texture here
				throw new Exception("Texture file doesn't exist");
			}

			if (texture == null)
			{
				texture = Content.Load<Texture2D>(path);
				Textures[path] = texture;
				Console.WriteLine("Loaded Texture: " + path);
			} 
			
			return texture;
		}

		public static Dictionary<string, T> LoadAsset<T>(ContentManager content, string assetDirectory,
			bool preload = false, Dictionary<string, T> collection = null)
		{
			if (collection == null)
			{
				collection = new Dictionary<string, T>();
			}

			string[] files = Directory.GetFiles(content.RootDirectory + "/" + assetDirectory);
			Console.WriteLine("Searching Directory: " + content.RootDirectory + "/" + assetDirectory);
			foreach (string file in files)
			{
				string fileName = Path.GetFileName(file).Split('.')[0];
				string contentName = fileName.Split('.')[0];
				if (preload)
				{
					T contentFile = content.Load<T>(assetDirectory + "/" + contentName);
					collection.Add(assetDirectory + "/" + contentName, contentFile);
					Console.WriteLine("Loaded Content: " + assetDirectory + "/" + contentName);
				}
				else
				{
					collection.Add(assetDirectory + "/" + contentName, default(T));
				}
			}

			string[] subDirectories = Directory.GetDirectories(content.RootDirectory + "/" + assetDirectory);
			foreach (string subDirectoryPath in subDirectories)
			{
				string subDirectory = new DirectoryInfo(subDirectoryPath).Name;
				assetDirectory += "/" + subDirectory;
				Console.WriteLine("Found Directory: " + subDirectory);
				Console.WriteLine("Relative Path: " + assetDirectory + "/");
				LoadAsset(content, assetDirectory, preload, collection);
			}

			return collection;
		}
	}
}

//
//        public static Dictionary<string, Dictionary<string, Texture2D>> Textures = new Dictionary<string, Dictionary<string, Texture2D>>();
//         public static List<Dictionary<string, Effect>> Effects;
//         public static List<Dictionary<string, SpriteFont>> Fonts;
//
// #if __ANDROID__
//         public static void LoadAndroidImages(ContentManager content)
//         {
//             foreach (string subDirectory in Game1.Activity.Assets.List("Content/Textures"))
//             {
//                 //List<string> subDirectorties = new List<string>();
//
//                 if (subDirectory.Contains('.'))
//                 {
//                     continue;
//                 }
//
//                 Textures.Add(subDirectory, new Dictionary<string, Texture2D>());
//
//                 foreach (string file in Game1.Activity.Assets.List("Content/Textures/" + subDirectory))
//                 {
//                     if (!file.Contains(".xnb"))
//                     {
//                         continue;
//                     }
//
//                     Texture2D texture = content.Load<Texture2D>("Textures/" + subDirectory + "/" + file.Split('.')[0]);
//                     Textures[subDirectory].Add(file.Split('.')[0], texture);
//                     Console.WriteLine("Loaded Texture: " + file);
//                 }
//             }
//
//             Console.WriteLine("Successfully Loaded All Android Texture Assets!");
//         }
// #endif
//
//         public static void LoadImages(ContentManager content)
//         {
//             foreach (string subDirectoryPath in Directory.GetDirectories(content.RootDirectory + "/Textures/"))
//             {
//                 //Texture2D texture = content.Load<Texture2D>("Textures/" + name);
//                 //Textures.Add(name, new Dictionary<string, Texture2D>());
//                 string subDirectory = new DirectoryInfo(subDirectoryPath).Name;
//                 Textures.Add(subDirectory, new Dictionary<string, Texture2D>());
//
//                 foreach (string path in Directory.GetFiles(content.RootDirectory + "/Textures/" + subDirectory))
//                 {
//                     string name = Path.GetFileName(path);
//                     //Console.WriteLine(name);
//                     Texture2D texture = content.Load<Texture2D>("Textures/" + subDirectory + "/" + name.Split('.')[0]);
//                     Textures[subDirectory].Add(name.Split('.')[0], texture);
//                     Console.WriteLine("Loaded Texture: " + name);
//                 }
//             }
//         }
//
//         public static void Load(ContentManager content)
// 		{
// #if __ANDROID__
//             LoadAndroidImages(content);
// #else
//             LoadImages(content);
// #endif
