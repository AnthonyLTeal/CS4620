using System;
using System.Collections.Generic;
using System.IO;
using CS4620IS.Components;
using ScottPlot;

namespace CS4620IS;

public readonly record struct ChartImageInfo(string Title, string ImagePath);

public static class SimulationChartSystem
{
    private const int ChartWidth = 720;
    private const int ChartHeight = 330;

    private static readonly Color BasicColor = Colors.DodgerBlue;
    private static readonly Color RerouteColor = Colors.OrangeRed;
    private static readonly Color TotalColor = Colors.ForestGreen;

    public static List<ChartImageInfo> GenerateCharts(SimulationSuper simSuper)
    {
        List<ChartImageInfo> results = new List<ChartImageInfo>();
        if (simSuper.BasicDestinationsReached.Count == 0)
            return results;

        string chartsDir = GetOutputDirectory();
        Directory.CreateDirectory(chartsDir);

        double[] groupCenters = GetGroupCenters(simSuper.BasicDestinationsReached.Count);
        string[] labels = BuildRunLabels(simSuper);

        results.Add(BuildTripletBarChart(
            "Destinations Reached",
            Path.Combine(chartsDir, "destinations_reached.png"),
            groupCenters,
            labels,
            ToDouble(simSuper.BasicDestinationsReached),
            ToDouble(simSuper.RerouteDestinationsReached),
            Sum(ToDouble(simSuper.BasicDestinationsReached), ToDouble(simSuper.RerouteDestinationsReached)),
            "Cars"));

        results.Add(BuildTripletBarChart(
            "Cars Generated",
            Path.Combine(chartsDir, "cars_generated.png"),
            groupCenters,
            labels,
            ToDouble(simSuper.TotalBasicCars),
            ToDouble(simSuper.TotalRerouteCars),
            Sum(ToDouble(simSuper.TotalBasicCars), ToDouble(simSuper.TotalRerouteCars)),
            "Cars"));

        results.Add(BuildTripletBarChart(
            "Distance Travelled",
            Path.Combine(chartsDir, "distance_travelled.png"),
            groupCenters,
            labels,
            ToDouble(simSuper.BasicDistancesTravelled),
            ToDouble(simSuper.RerouteDistancesTravelled),
            Sum(ToDouble(simSuper.BasicDistancesTravelled), ToDouble(simSuper.RerouteDistancesTravelled)),
            "Distance"));

        results.Add(BuildTripletBarChart(
            "Average Time To Destination",
            Path.Combine(chartsDir, "avg_time_to_destination.png"),
            groupCenters,
            labels,
            ToDouble(simSuper.BasicAverageTimesToDestinations),
            ToDouble(simSuper.RerouteAverageTimesToDestinations),
            AveragePair(ToDouble(simSuper.BasicAverageTimesToDestinations), ToDouble(simSuper.RerouteAverageTimesToDestinations)),
            "Seconds"));

        string congestionPath = Path.Combine(chartsDir, "average_congestion_over_time.png");
        BuildCongestionLineChart(simSuper, congestionPath);
        results.Add(new ChartImageInfo("Average Congestion Over Time", congestionPath));

        return results;
    }

    private static ChartImageInfo BuildTripletBarChart(
        string title,
        string filePath,
        double[] groupCenters,
        string[] labels,
        double[] basicValues,
        double[] rerouteValues,
        double[] totalValues,
        string yLabel)
    {
        Plot plot = new();

        Bar[] bars = BuildGroupedBars(groupCenters, basicValues, rerouteValues, totalValues);
        plot.Add.Bars(bars);

        plot.Legend.IsVisible = true;
        plot.Legend.Alignment = Alignment.UpperLeft;
        plot.Legend.ManualItems.Add(new LegendItem { LabelText = "Basic", FillColor = BasicColor });
        plot.Legend.ManualItems.Add(new LegendItem { LabelText = "Reroute", FillColor = RerouteColor });
        plot.Legend.ManualItems.Add(new LegendItem { LabelText = "Total", FillColor = TotalColor });

        Tick[] ticks = new Tick[groupCenters.Length];
        for (int i = 0; i < groupCenters.Length; i++)
        {
            ticks[i] = new Tick(groupCenters[i], labels[i]);
        }

        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
        plot.Axes.Bottom.MajorTickStyle.Length = 0;
        plot.HideGrid();
        plot.Axes.Margins(bottom: 0.15, left: 0.1, right: 0.1);
        plot.Title(title);
        plot.XLabel("Simulation Index + Basic:Reroute Distribution");
        plot.YLabel(yLabel);
        plot.SavePng(filePath, ChartWidth, ChartHeight);

        return new ChartImageInfo(title, filePath);
    }

    private static void BuildCongestionLineChart(SimulationSuper simSuper, string filePath)
    {
        Plot plot = new();

        for (int i = 0; i < simSuper.AverageCongestionCollectionLines.Count; i++)
        {
            List<(float congestion, float time)> line = simSuper.AverageCongestionCollectionLines[i];
            if (line.Count == 0)
                continue;

            double[] xs = new double[line.Count];
            double[] ys = new double[line.Count];
            for (int j = 0; j < line.Count; j++)
            {
                ys[j] = line[j].congestion;
                xs[j] = line[j].time;
            }

            var scatter = plot.Add.Scatter(xs, ys);
            scatter.LegendText = BuildDistributionLabel(simSuper, i);
        }

        plot.Legend.IsVisible = true;
        plot.Title("Average Congestion Over Time");
        plot.XLabel("Timestamp (s)");
        plot.YLabel("Average Congestion");
        plot.SavePng(filePath, ChartWidth, ChartHeight);
    }

    private static string[] BuildRunLabels(SimulationSuper simSuper)
    {
        int count = simSuper.BasicDestinationsReached.Count;
        string[] labels = new string[count];
        for (int i = 0; i < count; i++)
        {
            labels[i] = $"Sim {i + 1}\n{BuildDistributionLabel(simSuper, i)}";
        }

        return labels;
    }

    private static string BuildDistributionLabel(SimulationSuper simSuper, int index)
    {
        if (index < simSuper.DistributionRatiosBasic.Count)
        {
            float basicPercent = simSuper.DistributionRatiosBasic[index];
            float reroutePercent = 100 - basicPercent;
            return $"{basicPercent:0}%:{reroutePercent:0}%";
        }

        return "N/A";
    }

    private static Bar[] BuildGroupedBars(double[] groupCenters, double[] basic, double[] reroute, double[] total)
    {
        List<Bar> bars = new();
        const double offset = 0.7;
        const double barSize = 0.6;

        for (int i = 0; i < groupCenters.Length; i++)
        {
            double center = groupCenters[i];
            bars.Add(new Bar
            {
                Position = center - offset,
                Value = basic[i],
                Size = barSize,
                FillColor = BasicColor
            });
            bars.Add(new Bar
            {
                Position = center,
                Value = reroute[i],
                Size = barSize,
                FillColor = RerouteColor
            });
            bars.Add(new Bar
            {
                Position = center + offset,
                Value = total[i],
                Size = barSize,
                FillColor = TotalColor
            });
        }

        return bars.ToArray();
    }

    private static double[] GetGroupCenters(int runCount)
    {
        double[] centers = new double[runCount];
        for (int i = 0; i < runCount; i++)
        {
            centers[i] = i * 4 + 2;
        }

        return centers;
    }

    private static double[] ToDouble(List<int> values)
    {
        double[] output = new double[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            output[i] = values[i];
        }

        return output;
    }

    private static double[] ToDouble(List<float> values)
    {
        double[] output = new double[values.Count];
        for (int i = 0; i < values.Count; i++)
        {
            output[i] = values[i];
        }

        return output;
    }

    private static double[] Sum(double[] left, double[] right)
    {
        int length = Math.Min(left.Length, right.Length);
        double[] output = new double[length];
        for (int i = 0; i < length; i++)
        {
            output[i] = left[i] + right[i];
        }

        return output;
    }

    private static double[] AveragePair(double[] left, double[] right)
    {
        int length = Math.Min(left.Length, right.Length);
        double[] output = new double[length];
        for (int i = 0; i < length; i++)
        {
            output[i] = (left[i] + right[i]) / 2.0;
        }

        return output;
    }

    public static string GetOutputDirectory()
    {
        return Path.Combine(AppContext.BaseDirectory, "SimulationCharts");
    }
}
