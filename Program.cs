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
using System.Text.RegularExpressions;
using System.Numerics;
using System.Threading;
using System.Timers;
using CS4620IS.Collision;
using CS4620IS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PlanetaryExpansion;
using ScottPlot;
using Vector3 = Microsoft.Xna.Framework.Vector3;

using var game = new CS4620IS.Game1();


game.Run();

//put this into the buttons once you merge everything together, remember this returns a list of the files created
//Right now, this only includes the 2 population plots (alive times and wait times), but it will include
//Price of Anarchy once it is implemented. 
Graph.GenerateGraphs();


