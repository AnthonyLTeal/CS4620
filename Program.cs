using var game = new CS4620IS.Game1();
game.Run();
//graph stuff testing
/*
ScottPlot.Plot signalPlot = new();
signalPlot.Add.Signal(CarSystems.finalDestinationTimes);
signalPlot.Title("Times Took For Cars To Reach Destination");
string root = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
string graphsDir = Path.Combine(root, "Graphs");
Directory.CreateDirectory(graphsDir);
string path = Path.Combine(graphsDir, "firstrun.png");
signalPlot.XLabel("Car");
signalPlot.YLabel("Destination Time (In Seconds");
signalPlot.SavePng(path, 400, 300);
CarSystems.finalDestinationTimes.Clear();
*/