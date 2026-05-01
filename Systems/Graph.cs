using System;
using System.Collections.Generic;
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

namespace CS4620IS;

public class Graph
{
   public static List<string> GenerateGraphs()
   {
       //This function creates the 2 population plots, and the Price of Anarchy Plot
       //This function returns the file path of each of these graphs in a string list so you can load them.
       //They get saved to the Graphs folder in this function, so you do not need to save these after calling the function.
       
       SimulationSuper simulationSuper = EntityManager.GetGlobalComponent<SimulationSuper>();
       //testing only, comment this back in to see if graph points line up
       //Console.WriteLine("Count of Control Group:" + simulationSuper.ControlGroup.Count);
       //Console.WriteLine("Count of Rerouting Group:" + simulationSuper.RerouteGroup.Count);

       //Creates the Graph Directory If It Does not Exist
       string root = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
       string graphsDirectory = Path.Combine(root, "Graphs");
       Directory.CreateDirectory(graphsDirectory);
      
       //Creating The Plots and Other Variables
       Plot alivePlot = new();
       Plot waitTimesPlot = new();
       Plot demoPriceofAnarchy = new();
       bool control_group_plot = true;
       bool reroute_group_plot = true;
       
       
       
       for (int i = 0; i < 2; i++)
       {
        //*If there are no cars in a group, it will not plot the group on the chart (since there are no values in the group)*
        //This means that 100% static cars or 100% rerouting cars will still work with these plots, but only 1 group will be plotted
        //On the charts since only 1 group has data.
        
           //adding control group data to graphs, this will be the first group on the plots.
           if (i == 0)
           {
            if (simulationSuper.ControlGroup.Count > 0)
            {
             double[] avalues = simulationSuper.ControlGroup
              .Select(car => car.AliveTime)
              .ToArray();
             double[] wvalues = simulationSuper.ControlGroup
              .Select(car => car.WaitTime)
              .ToArray();
             var apopulation = alivePlot.Add.Population(avalues, x: i);
             var wpopulation = waitTimesPlot.Add.Population(wvalues, x: i);
             apopulation.Marker.Shape = MarkerShape.HorizontalBar;
             wpopulation.Marker.Shape = MarkerShape.HorizontalBar;
            }
           }

           //adding reroute group data to graphs, this will be the second group on the plots.
           //Anthony, if you haven't already, make sure that in the master branch that Matthew's button that assigns
           //cars put the cars in the right bins in simulationSuper, otherwise these graphs will be incorrect
           if (i == 1)
           {
            if (simulationSuper.RerouteGroup.Count > 0)
            {
              double[] avalues = simulationSuper.RerouteGroup
               .Select(car => car.AliveTime)
               .ToArray();
              double[] wvalues = simulationSuper.RerouteGroup
               .Select(car => car.WaitTime)
               .ToArray();
              var apopulation = alivePlot.Add.Population(avalues, x: i);
              var wpopulation = waitTimesPlot.Add.Population(wvalues, x: i);
            }
           }
           //testing these 2 lines
           //apopulation.Bar.ErrorPositive = true;
           //apopulation.Bar.ErrorNegative = true;
           //population.Marker.Shape = MarkerShape.; (this changes the shape of the points)
       }
        // make the bottom of the plot snap to zero by default
       alivePlot.Axes.Margins(bottom: 0);
       waitTimesPlot.Axes.Margins(bottom: 0);
       

        // the ticklabels will be the 2 groups we are having, and there will be 2 tickpositions, one for each group.
        // right now, they are the defualt values
        
       double[] tickPositions = Generate.Consecutive(2);
       string[] tickLabels = ["Non-Rerouting Cars", "Rerouting Cars"];

       alivePlot.Axes.Bottom.SetTicks(tickPositions, tickLabels);
       waitTimesPlot.Axes.Bottom.SetTicks(tickPositions, tickLabels);

        // this is from the website to get a graph that looks like the one in the example graphs
       alivePlot.Axes.Bottom.MajorTickStyle.Length = 0;
       waitTimesPlot.Axes.Bottom.MajorTickStyle.Length = 0;

       alivePlot.Axes.Margins(bottom: 0);
       waitTimesPlot.Axes.Margins(bottom: 0);

       alivePlot.HideGrid();
       waitTimesPlot.HideGrid();

       alivePlot.YLabel("Time Alive (In Seconds)");
       waitTimesPlot.YLabel("Time Spent Waiting at Intersections or In Traffic (In Seconds)");
       
       //Now that the population plots are done, it will now create the Price of Anarchy Graph.
       
       //Anthony, here is a demo for how a signal plot will get created in ScottPlot (which is what the Price of Anarchy plot will be)
       //I will change a bit of the code when you get the stuff created, but it will not take long
       //as long as I have the values.
       string path = graphsDirectory;
       ScottPlot.Plot myPlot = new();
       double[] values = Generate.RandomWalk(1_000_000);
       myPlot.Add.Signal(values);
       myPlot.Title("Signal Plot with 1 Million Points");
       path = Path.Combine(graphsDirectory, "demo.png");
       myPlot.SavePng(path, 400, 300);
       

        //This is what generates the names for the saving of each population plot.
        //The Price of Anarchy graph will get saved in nearly the same way as this once it is in.
        
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
       List<string> pathsList = new List<string>();
       path = Path.Combine(graphsDirectory, $"aliveTimesSimulation{current_simulation}.png");
       pathsList.Add(path);
       alivePlot.SavePng(path, 400, 300);

       path = Path.Combine(graphsDirectory, $"waitTimesSimulation{current_simulation}.png");
       waitTimesPlot.SavePng(path, 400, 300);
       pathsList.Add(path);
    
        //resetting the cars for each simulation
       simulationSuper.ControlGroup.Clear();
       return pathsList;
   } 
}