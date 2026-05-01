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

//Graph things, remove it from here and put it into the UI tomorrow
SimulationSuper simulationSuper = EntityManager.GetGlobalComponent<SimulationSuper>();
//testing only, comment this back in to see if graph points line up
//Console.WriteLine("Count of Control Group:" + simulationSuper.ControlGroup.Count);

//Saving images stuff
string root = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
string graphsDirectory = Path.Combine(root, "Graphs");
Directory.CreateDirectory(graphsDirectory);

//Creating the plots
Plot alivePlot = new();
Plot intersectionDelayPlot = new();
Plot trafficDelayPlot = new();

for (int i = 0; i < 5; i++)
{
    //control group times (rewrite to be only when i = 0 once all groups are in)
    double[] avalues = simulationSuper.ControlGroup
    .Select(car => car.AliveTime)
    .ToArray();
    double[] ivalues = simulationSuper.ControlGroup
        .Select(car => car.SignDelayTime)
        .ToArray();
    double[] tvalues = simulationSuper.ControlGroup
        .Select(car => car.CarDelayTime)
        .ToArray();
    var apopulation = alivePlot.Add.Population(avalues, x: i);
    var ipopulation = intersectionDelayPlot.Add.Population(ivalues, x: i);
    var tpopulation = trafficDelayPlot.Add.Population(tvalues, x: i);
    //add i = 1 for rerouting group and i = 2 for partial group

    apopulation.Marker.Shape = MarkerShape.HorizontalBar;
    ipopulation.Marker.Shape = MarkerShape.HorizontalBar;
    tpopulation.Marker.Shape = MarkerShape.HorizontalBar;
    
    //testing these 2 lines
    //apopulation.Bar.ErrorPositive = true;
    //apopulation.Bar.ErrorNegative = true;
    //population.Marker.Shape = MarkerShape.; (this changes the shape of the points)
}
// make the bottom of the plot snap to zero by default
alivePlot.Axes.Margins(bottom: 0);
intersectionDelayPlot.Axes.Margins(bottom: 0);
trafficDelayPlot.Axes.Margins(bottom: 0);

// the ticklabels will be the 3 groups we are having, and there will be 3 tickpositions, one for each group.
// right now, they are the defualt values
double[] tickPositions = Generate.Consecutive(5);
string[] tickLabels = Enumerable.Range(1, 5).Select(x => $"Group {x}").ToArray();

alivePlot.Axes.Bottom.SetTicks(tickPositions, tickLabels);
intersectionDelayPlot.Axes.Bottom.SetTicks(tickPositions, tickLabels);
trafficDelayPlot.Axes.Bottom.SetTicks(tickPositions, tickLabels);

// this is from the website to get a graph that looks like the one in the example graphs
alivePlot.Axes.Bottom.MajorTickStyle.Length = 0;
intersectionDelayPlot.Axes.Bottom.MajorTickStyle.Length = 0;
trafficDelayPlot.Axes.Bottom.MajorTickStyle.Length = 0;

alivePlot.Axes.Margins(bottom: 0);
intersectionDelayPlot.Axes.Margins(bottom: 0);
trafficDelayPlot.Axes.Margins(bottom: 0);

alivePlot.HideGrid();
intersectionDelayPlot.HideGrid();
trafficDelayPlot.HideGrid();

alivePlot.YLabel("Time Alive (In Seconds)");
intersectionDelayPlot.YLabel("Time Spent at Intersections (In Seconds)");
trafficDelayPlot.YLabel("Time Spent Behind Other Cars (In Seconds)");

//This is new, this is what generates the names for the saving of each graph
string path;
//this gets all of the file names in the Graphs folder, is also used below.
var files = Directory.EnumerateFiles(graphsDirectory);
//this is what changes between plots in the Graphs folder, and this is used below.
//the \d+ just means any digit, which is the last part of the format I save the graphs in.
var simulationNumberPattern = new Regex(@"Simulation(\d+)", RegexOptions.IgnoreCase);


//This mini function gets the most recent simulation number from the plots
//already in the Graphs folder, by checking if it matches the regex defined above (which all do). 
//It returns the maximum number/most recent simulation ran, which is the plot in the Graphs folder
//with the highest number at the end of its name.
//If no graphs are there (which means no simulation has been ran before), the most recent simulation is defaulted to 0.
int mostrecentSimulation = files
    .Select(file => simulationNumberPattern.Match(Path.GetFileName(file)))
    .Where(match => match.Success)
    .Select(match => int.Parse(match.Groups[1].Value))
    .DefaultIfEmpty(0)
    .Max();


//saving the plots to be updated to the current simulation, that way no previous plots are overwritten.
int current_simulation = mostrecentSimulation + 1;
path = Path.Combine(graphsDirectory, $"aliveTimesSimulation{current_simulation}.png");
alivePlot.SavePng(path, 400, 300);

path = Path.Combine(graphsDirectory, $"intersectionDelayTimesSimulation{current_simulation}.png");
intersectionDelayPlot.SavePng(path, 400, 300);

path = Path.Combine(graphsDirectory, $"trafficDelayTimesSimulation{current_simulation}.png");
trafficDelayPlot.SavePng(path, 400, 300);

//resetting the cars for each simulation
simulationSuper.ControlGroup.Clear();



