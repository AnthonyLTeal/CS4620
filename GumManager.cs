using MonoGameGum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using System;
using System.Collections;
using Gum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using System.ComponentModel;
using Gum.Forms;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.IO;
using PlanetaryExpansion;
using RenderingLibrary.Graphics;

namespace CS4620IS;

class GumInterface
{
    public void InitializeUI()
    {
        GumService.Default.Root.Children.Clear();
        CreateStartPanel();
        SimulationSystems.Clear();
        SimulationSystems.SetSpeed(2);
    }

    private void CreateStartPanel()
    {
        //Creating the start panel that will hold all of the buttons for the UI
        StackPanel StartPanel = new StackPanel();
        StartPanel.Spacing = 5;
        StartPanel.Anchor(Anchor.TopLeft);
        StartPanel.AddToRoot();

    //1. Save System
        Window SaveWindow = CreateWindow();
        SaveWindow.Height = 400;
        SaveWindow.AddToRoot();
        SaveWindow.IsVisible = false;

        // Add a Panel to the window 
        StackPanel SavePanel = new StackPanel();
        SavePanel.Spacing = 3;
        //SavePanel.Dock(Dock.Fill);
        SavePanel.Anchor(Anchor.Center);
        SaveWindow.AddChild(SavePanel);

        //Add Label to Panel
        Label SaveLabel = new Label();
        SaveLabel.Text = "File Name: ";
        //SaveLabel.Anchor(Anchor.Center);
        //SaveLabel.Anchor(Anchor.Top);
        SavePanel.AddChild(SaveLabel);

        // Add Textbox to the panel
        TextBox inputBox = new TextBox();
        inputBox.Text = "";
        //inputBox.Anchor(Anchor.Center);
        //inputBox.Anchor(Anchor.Top);
        SavePanel.AddChild(inputBox);

        Label FileLabel = new Label();
        FileLabel.Text = "Select file to save";
        SavePanel.AddChild(FileLabel);

        //Create a ListBox with all of the file information
        ListBox SaveBox = new ListBox();
        SavePanel.AddChild(SaveBox);

        // Add Button to Save Road Data:
        Button SButton = new Button();
        SButton.Text = "Save File";
        SavePanel.AddChild(SButton);

        if (!Directory.Exists("saves"))
        {
            Directory.CreateDirectory("saves");
        }

        foreach (var fileName in Directory.EnumerateFiles("saves"))
        {
            SaveBox.Items.Add(Path.GetFileNameWithoutExtension(fileName));
        }

        SaveBox.ItemClicked += (sender, args) =>
        {
            inputBox.Text = SaveBox.SelectedObject.ToString();
        };

        SButton.Click += (sender, args) =>
        {
            if (inputBox.Text == "")
                return;

            string fileName = inputBox.Text;
            SaveSystem.Save("saves/" + fileName + ".ism");
            SaveWindow.IsVisible = false;
        };

        Button SaveButton = new Button();
        SaveButton.Text = "Save";
        StartPanel.AddChild(SaveButton); // Add the Save button as a child to the StartPanel
        SaveButton.Click += (sender, args) =>
        {
            SaveWindow.IsVisible = true;
        };

        // Add Cancel Button to the Window:
        Button CloseWindowButton = new Button();
        CloseWindowButton.Text = "Cancel";
        SavePanel.AddChild(CloseWindowButton);
        CloseWindowButton.Click += (sender, args) =>
        {
            SaveWindow.IsVisible = false; ;
        };

    //2. Load System
        Window LoadWindow = CreateWindow();
        LoadWindow.Height = 350;
        LoadWindow.AddToRoot();
        LoadWindow.IsVisible = false;

        StackPanel LoadPanel = new StackPanel();
        LoadPanel.Anchor(Anchor.Center);
        LoadPanel.Spacing = 4;
        LoadWindow.AddChild(LoadPanel);

        Label LoadLabel = new Label();
        LoadLabel.Text = "Select which road system to open";
        LoadPanel.AddChild(LoadLabel);

        if (!Directory.Exists("saves"))
        {
            Directory.CreateDirectory("saves");
        }

        //Creating a List Box to view files
        ListBox LoadBox = new ListBox();
        LoadPanel.AddChild(LoadBox);

        foreach (var fileName in Directory.EnumerateFiles("saves"))
        {
            LoadBox.Items.Add(Path.GetFileNameWithoutExtension(fileName));
        }

        //Create a Button to load once file is selected from listBox:
        Button LButton = new Button();
        LButton.Text = "Load";
        LoadPanel.AddChild(LButton);
        LButton.Click += (sender, args) =>
        {
            // LoadSystem.Load(ListBox.SelectedStateName);
            LoadSystem.Load("saves/" + LoadBox.SelectedObject + ".ism");
            LoadWindow.IsVisible = false;
        };

        Button CancelButton = new Button();
        CancelButton.Text = "Cancel";
        LoadPanel.AddChild(CancelButton);
        CancelButton.Click += (sender, args) =>
        {
            LoadWindow.IsVisible = false;
        };

        Button LoadButton = new Button();
        LoadButton.Text = "Load";
        StartPanel.AddChild(LoadButton);
        LoadButton.Click += (sender, args) =>
        {
            Console.WriteLine("Clicked on the load button!");
            LoadWindow.IsVisible = true;
        };

    //3. Add button 3 -> ExitButton
        Button ExitButton = new Button();
        ExitButton.Text = "Exit";
        StartPanel.AddChild(ExitButton);
        ExitButton.Click += (sender, args) =>
        {
            Console.WriteLine("Clicked on the exit button!");
            Environment.Exit(0);
        };

    // 4. Add Road Path Button:
        Button RoadPathButton = new Button();
        RoadPathButton.Text = "Draw Road False";
        StartPanel.AddChild(RoadPathButton);
        RoadPathButton.Click += (sender, args) =>
        {
            Console.WriteLine("Clicked on the road path button!");
            // Logic to enable road path editing mode goes here
            RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
            roadMesh.CreateEnabled = !roadMesh.CreateEnabled;
            RoadPathButton.Text = "Draw Road " + roadMesh.CreateEnabled;

        };

    //5. Road Path Type
        Button PathTypeButton = new Button();
        PathTypeButton.Text = "Path Type: Straight";
        StartPanel.AddChild(PathTypeButton);

        PathTypeButton.Click += (sender, args) =>
        {
            RoadMesh roadMesh = EntityManager.GetGlobalComponent<RoadMesh>();
            if (roadMesh.BuildType == BuildType.Curved)
                roadMesh.BuildType = BuildType.Straight;
            else
                roadMesh.BuildType = BuildType.Curved;

            PathTypeButton.Text = "Path Type:  " + roadMesh.BuildType;
        };

        Button ClearButton = new Button();
        ClearButton.Text = "Clear Simulation";
        StartPanel.AddChild(ClearButton);
        ClearButton.Click += (sender, args) =>
        {
            Console.WriteLine("Clicked on the clear simulation button!");
            SimulationSystems.Clear();
        };

    // 6. Chart viewing and selection:
        Window GraphWindow = CreateWindow();
        GraphWindow.AddToRoot();
        GraphWindow.IsVisible = false;

        StackPanel ChartPanel = new StackPanel();
        ChartPanel.Spacing = 20;
        ChartPanel.Anchor(Anchor.Center);
        GraphWindow.AddChild(ChartPanel);

        Button GraphWindowButton = new Button();
        GraphWindowButton.Text = "View Charts";
        StartPanel.AddChild(GraphWindowButton);
        GraphWindowButton.Click += (sender, args) =>
        {
            GraphWindow.IsVisible = true;
        };

        //Chart 1:
        Window Chart1Window = CreateWindow();
        Chart1Window.Width = 460;
        Chart1Window.Height = 360;
        Chart1Window.AddToRoot();
        Chart1Window.IsVisible = false;

        //Add the chart image to the window:
        SpriteRuntime chartSprite = new SpriteRuntime();
        // The Source file name, has to be a specific path on the user's machine at the moment.
        chartSprite.SourceFileName = "C:\\Users\\inter\\OneDrive\\Desktop\\Spring 2026\\CS4620 Intelligent Systems\\New folder\\CS4620\\Graphs\\Graph_20260409_083621.png";
        chartSprite.Dock(Dock.Fill);

        // Creating Rectangle for the image to sit in:
        RectangleRuntime chart1Rectangle = new RectangleRuntime();
        chart1Rectangle.Width = 400;
        chart1Rectangle.Height = 300;
        chart1Rectangle.Anchor(Anchor.Center);
        chart1Rectangle.AddChild(chartSprite);
        Chart1Window.AddChild(chart1Rectangle);

        //Add Exit button to the graph window:
        Button ExitGraphButton = new Button();
        ExitGraphButton.Text = "Cancel";
        ExitGraphButton.Anchor(Anchor.TopRight);
        Chart1Window.AddChild(ExitGraphButton);
        ExitGraphButton.Click += (sender, args) =>
        {
            Chart1Window.IsVisible = false;
        };

        Button Chart1Button = new Button();
        Chart1Button.Text = "Chart 1";
        ChartPanel.AddChild(Chart1Button);
        Chart1Button.Click += (sender, args) =>
        {
            Chart1Window.IsVisible = true;
        };

        //Chart 2:
        Window Chart2Window = CreateWindow();
        Chart2Window.Width = 460;
        Chart2Window.Height = 360;
        Chart2Window.AddToRoot();
        Chart2Window.IsVisible = false;
        SpriteRuntime chart2Sprite = new SpriteRuntime();
        chart2Sprite.SourceFileName = "C:\\Users\\inter\\OneDrive\\Desktop\\Spring 2026\\CS4620 Intelligent Systems\\New folder\\CS4620\\Graphs\\Graph_20260409_083621.png";

        RectangleRuntime chart2Rectangle = new RectangleRuntime();
        chart2Rectangle.Width = 400;
        chart2Rectangle.Height = 300;
        chart2Rectangle.Anchor(Anchor.Center);
        chart2Rectangle.AddChild(chart2Sprite);
        Chart2Window.AddChild(chart2Rectangle);

        Button ExitChart2Button = new Button();
        ExitChart2Button.Text = "Cancel";
        ExitChart2Button.Anchor(Anchor.TopRight);
        Chart2Window.AddChild(ExitChart2Button);
        ExitChart2Button.Click += (sender, args) =>
        {
            Chart2Window.IsVisible = false;
        };

        Button Chart2Button = new Button();
        Chart2Button.Text = "Chart 2";
        ChartPanel.AddChild(Chart2Button);
        Chart2Button.Click += (sender, args) =>
        {
            Chart2Window.IsVisible = true;
        };

        //Chart 3
        Window Chart3Window = CreateWindow();
        Chart3Window.Width = 460;
        Chart3Window.Height = 360;
        Chart3Window.AddToRoot();
        Chart3Window.IsVisible = false;
        SpriteRuntime chart3Sprite = new SpriteRuntime();
        chart3Sprite.SourceFileName = "C:\\Users\\inter\\OneDrive\\Desktop\\Spring 2026\\CS4620 Intelligent Systems\\New folder\\CS4620\\Graphs\\Graph_20260409_083621.png";

        RectangleRuntime chart3Rectangle = new RectangleRuntime();
        chart3Rectangle.Width = 400;
        chart3Rectangle.Height = 300;
        chart3Rectangle.Anchor(Anchor.Center);
        chart3Rectangle.AddChild(chart3Sprite);
        Chart3Window.AddChild(chart3Rectangle);

        Button ExitChart3Button = new Button();
        ExitChart3Button.Text = "Cancel";
        ExitChart3Button.Anchor(Anchor.TopRight);
        Chart3Window.AddChild(ExitChart3Button);
        ExitChart3Button.Click += (sender, args) =>
        {
            Chart3Window.IsVisible = false;
        };

        Button Chart3Button = new Button();
        Chart3Button.Text = "Chart 3";
        ChartPanel.AddChild(Chart3Button);
        Chart3Button.Click += (sender, args) =>
        {
            Chart3Window.IsVisible = true;

        };

        //Add Exit button to the chart selection window:
        Button ExitGraphWindowButton = new Button();
        ExitGraphWindowButton.Text = "Cancel";
        ChartPanel.AddChild(ExitGraphWindowButton);
        ExitGraphWindowButton.Click += (sender, args) =>
        {
            GraphWindow.IsVisible = false;
        };

    //7. Running the Simulation:
        //Add Window for controls
        Window SpawnWindow = CreateWindow();
        SpawnWindow.AddToRoot();
        SpawnWindow.IsVisible = false;

        StackPanel SpawnPanel = new StackPanel();
        SpawnPanel.Spacing = 4;
        SpawnPanel.Anchor(Anchor.Center);
        SpawnWindow.AddChild(SpawnPanel);

        //Create Labels and TextBoxes for Static and Dynamic Cars:
        Label CarCountLabel = new Label();
        CarCountLabel.Text = "Total cars";
        SpawnPanel.AddChild(CarCountLabel);
        TextBox CarCountTextbox = new TextBox();
        SpawnPanel.AddChild(CarCountTextbox);

        Label PercentLabel = new Label();
        PercentLabel.Text = "Percent of cars that are basic vs reroute";
        SpawnPanel.AddChild(PercentLabel);
        TextBox PercentTextbox = new TextBox();
        SpawnPanel.AddChild(PercentTextbox);

        Label SeedLabel = new Label();
        SeedLabel.Text = "Seed";
        SpawnPanel.AddChild(SeedLabel);
        TextBox SeedTextbox = new TextBox();
        SpawnPanel.AddChild(SeedTextbox);

        Label SimTimeLabel = new Label();
        SimTimeLabel.Text = "Simulation Run time;";
        SpawnPanel.AddChild(SimTimeLabel);
        TextBox TimeBox = new TextBox();
        SpawnPanel.AddChild(TimeBox);

        //Create Button to Start Simulation:
        Button StartSimButton = new Button();
        StartSimButton.Text = "Start Simulation";
        SpawnPanel.AddChild(StartSimButton);
        StartSimButton.Click += (sender, args) =>
        {
            int totalCount;
            int distributionPercent;
            int seed;

            if (int.TryParse(CarCountTextbox.Text, out totalCount) &&
                int.TryParse(PercentTextbox.Text, out distributionPercent) &&
                int.TryParse(SeedTextbox.Text, out seed))
            {
                Console.WriteLine($"Spawning {totalCount} static cars and {distributionPercent}% of them are dynamic cars");
                // Call your car spawning logic here using staticCount and dynamicCount
                //CarSystems.GenerateRandomCar();
                CarSystems.GenerateCars(totalCount, seed, distributionPercent * .01f);
                SpawnWindow.RemoveFromRoot();
            }
            else
            {
                Console.WriteLine("Invalid input for static or dynamic car count. Please enter valid integers.");
            }
        };

        // Create Exit Button for Spawn Window:
        Button ExitSpawnButton = new Button();
        ExitSpawnButton.Text = "Cancel";
        SpawnPanel.AddChild(ExitSpawnButton);
        ExitSpawnButton.Click += (sender, args) =>
            {
                SpawnWindow.IsVisible = false;
            };

        Button runButton = new Button();
        runButton.Text = "Run simulation";
        StartPanel.AddChild(runButton);
        runButton.Click += (sender, args) =>
        {
            SpawnWindow.IsVisible = true;
        };

    // 8. Selecting the Speed
        ColoredRectangleRuntime SpeedRectangle = new ColoredRectangleRuntime();
        SpeedRectangle.Color = Microsoft.Xna.Framework.Color.DarkGray;
        SpeedRectangle.Width = 127;
        SpeedRectangle.Height = 100;
        StartPanel.AddChild(SpeedRectangle);

        StackPanel RectangleStackPanel = new StackPanel();
        RectangleStackPanel.Spacing = 4;
        RectangleStackPanel.Dock(Dock.Fill);
        SpeedRectangle.AddChild(RectangleStackPanel);

        Label SpeedLabel = new Label();
        SpeedLabel.Text = " Simulation \n Speed: ";
        RectangleStackPanel.AddChild(SpeedLabel);

        StackPanel SpeedPanel = new StackPanel();
        SpeedPanel.Spacing = 4;
        RectangleStackPanel.AddChild(SpeedPanel);
        SpeedPanel.Orientation = Orientation.Horizontal;

        Button Speed1Button = new Button();
        Speed1Button.Text = "1x";
        Speed1Button.Width = 30;
        Speed1Button.Height = 15;
        SpeedPanel.AddChild(Speed1Button);
        Speed1Button.Click += (sender, args) =>
        {
            SimulationSystems.SetSpeed(1);
        };

        Button Speed2Button = new Button();
        Speed2Button.Text = "2x";
        Speed2Button.Width = 30;
        Speed2Button.Height = 15;
        SpeedPanel.AddChild(Speed2Button);
        Speed2Button.Click += (sender, args) =>
        {
            SimulationSystems.SetSpeed(2);
        };

        Button Speed4Button = new Button();
        Speed4Button.Text = "4x";
        Speed4Button.Width = 30;
        Speed4Button.Height = 15;
        SpeedPanel.AddChild(Speed4Button);
        Speed4Button.Click += (sender, args) =>
        {
            SimulationSystems.SetSpeed(4);
        };

    // 9. Weather --> Doesn't do anything right now, maybe we'll add some later or delete it.
        Window WeatherWindow = CreateWindow();
        WeatherWindow.AddToRoot();
        WeatherWindow.IsVisible = false;
        StackPanel WeatherPanel = new StackPanel();
        WeatherPanel.Spacing = 4;
        WeatherPanel.Anchor(Anchor.Center);
        WeatherWindow.AddChild(WeatherPanel);
        WeatherPanel.AddChild(new Label { Text = "Weather Options" });

        //Create Slider for Rain Intensity:
        WeatherPanel.AddChild(new Label { Text = "Rain Intensity" });
        Slider RainSlider = CreateSlider(); ;
        WeatherPanel.AddChild(RainSlider);

        //Create Slider for Snow Intensity:
        WeatherPanel.AddChild(new Label { Text = "Snow Intensity" });
        Slider SnowSlider = CreateSlider();
        WeatherPanel.AddChild(SnowSlider);
        Button WeatherButton = new Button();

        // StartPanel.addchild(WeatherButton);
        WeatherButton.Text = "Weather options";
        WeatherButton.Click += (sender, args) =>
        {
            WeatherWindow.IsVisible = true;
        };

    // 10. Adding Signs: Also doesn't do anything right now, but we can add functionality later or delete it.
        Button AddSignButton = new Button();
        AddSignButton.Text = "Add Sign";
        //StartPanel.AddChild(AddSignButton);
        AddSignButton.Click += (sender, args) =>
        {
            Console.WriteLine("Clicked on the add sign button!");
            // Logic to enable adding a sign goes here
        };

    }

    private Window CreateWindow()  // Used to create a basic window, maybe not strictly necessary, but found it handy. 
    {
        Window NewWindow = new Window();
        NewWindow.Anchor(Anchor.Center);
        NewWindow.Width = 400;
        NewWindow.Height = 300;
        return NewWindow;

    }

    private Slider CreateSlider()   //Create Slider is just used in the weather system
    {
        Slider newSlider = new Slider();
        newSlider.AddToRoot();
        newSlider.Maximum = 30;
        newSlider.Minimum = 0;
        newSlider.TicksFrequency = 1;
        newSlider.IsSnapToTickEnabled = true;
        newSlider.Width = 150;
        newSlider.ValueChanged += (sender, args) =>
        {
            Console.WriteLine($"Slider value changed to: {newSlider.Value}");
        };
        return newSlider;
    }
}