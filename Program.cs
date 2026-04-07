using System;
using CS4620IS;
using CS4620IS.Components;

//delete this after testing
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Timers;
using CS4620IS.Collision;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlanetaryExpansion;
using Vector3 = Microsoft.Xna.Framework.Vector3;

using var game = new CS4620IS.Game1();
game.Run();
//graph stuff testing
//ScottPlot.Plot signalPlot = new();
//signalPlot.Add.Signal(CarSystems.finalDestinationTimes);
//signalPlot.Title("Times Took For Cars To Reach Destination");
//string root = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
//string graphsDir = Path.Combine(root, "Graphs");
//Directory.CreateDirectory(graphsDir);
//string path = Path.Combine(graphsDir, "firstrun.png");
//signalPlot.XLabel("Car");
//signalPlot.YLabel("Destination Time (In Seconds");
//signalPlot.SavePng(path, 400, 300);

//CarSystems.finalDestinationTimes.Clear();
