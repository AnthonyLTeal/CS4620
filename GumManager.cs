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
using CS4620IS.Components;
using PlanetaryExpansion;
using Microsoft.Xna.Framework.Graphics;
//using RenderingLibrary.Graphics;
//using System.IO.Enumeration;

namespace CS4620IS;


class GumInterface
{
    private const int ChartWindowWidth = 780;   // <= 65% of 1280
    private const int ChartWindowHeight = 480;  // extra room for controls
    private const int ChartImageMaxWidth = 720;
    private const int ChartImageMaxHeight = 330;

    public static GumInterface Instance { get; private set; }

    private Window _saveWindow;
    private TextBox _saveInputBox;
    private ListBox _saveListBox;

    private Window _loadWindow;
    private ListBox _loadListBox;

    private Window _runSimulationWindow;
    private Label _runSimulationStatusLabel;
    private TextBox _carCountTextbox;
    private TextBox _percentTextbox;
    private TextBox _seedTextbox;
    private TextBox _maxSimTimeTextbox;
    private TextBox _distributionStepTextbox;
    private readonly List<Window> _chartWindows = new List<Window>();

    public void InitializeUI()
    {
        Instance = this;
        GumService.Default.Root.Children.Clear();
        _saveWindow = null;
        _saveInputBox = null;
        _saveListBox = null;
        _loadWindow = null;
        _loadListBox = null;
        _runSimulationWindow = null;
        _runSimulationStatusLabel = null;
        _carCountTextbox = null;
        _percentTextbox = null;
        _seedTextbox = null;
        _maxSimTimeTextbox = null;
        _distributionStepTextbox = null;
        _chartWindows.Clear();
        CreateStartPanel();
        SimulationSystems.Clear();
    }

    public void ShowSimulationCharts(List<ChartImageInfo> charts)
    {
        if (charts == null || charts.Count == 0)
            return;

        GraphicsDevice graphicsDevice = ComponentManager.GetGlobalComponent<GraphicsDevice>();
        if (graphicsDevice == null)
            return;

        foreach (Window oldWindow in _chartWindows)
        {
            oldWindow.RemoveFromRoot();
        }
        _chartWindows.Clear();

        for (int i = 0; i < charts.Count; i++)
        {
            ChartImageInfo chart = charts[i];
            if (!File.Exists(chart.ImagePath))
                continue;

            Window chartWindow = CreateWindow();
            chartWindow.Width = ChartWindowWidth;
            chartWindow.Height = ChartWindowHeight;
            chartWindow.X = -220 + (i % 2) * 120;
            chartWindow.Y = -130 + (i / 2) * 30;
            chartWindow.Visual.ClipsChildren = true;

            StackPanel panel = new StackPanel();
            panel.Spacing = 6;
            panel.Anchor(Anchor.TopLeft);
            panel.X = 16;
            panel.Y = 12;
            chartWindow.AddChild(panel);

            Label title = new Label();
            title.Text = chart.Title;
            panel.AddChild(title);

            Texture2D chartTexture = ImageLoader.LoadImage(graphicsDevice, chart.ImagePath);
            int imageWidth = chartTexture.Width;
            int imageHeight = chartTexture.Height;
            float scale = 1;
            if (imageWidth > 0 && imageHeight > 0)
            {
                float xScale = (float)ChartImageMaxWidth / imageWidth;
                float yScale = (float)ChartImageMaxHeight / imageHeight;
                scale = Math.Min(1f, Math.Min(xScale, yScale));
            }

            int finalWidth = Math.Max(1, (int)(imageWidth * scale));
            int finalHeight = Math.Max(1, (int)(imageHeight * scale));

            SpriteRuntime chartSprite = new SpriteRuntime();
            chartSprite.Texture = chartTexture;
            chartSprite.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            chartSprite.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            chartSprite.TextureWidth = imageWidth;
            chartSprite.TextureHeight = imageHeight;
            chartSprite.Width = finalWidth;
            chartSprite.Height = finalHeight;
            chartSprite.X = 16;
            chartSprite.Y = 48;
            chartWindow.Visual.AddChild(chartSprite);

            Button closeButton = new Button();
            closeButton.Text = "Close";
            closeButton.X = 16;
            closeButton.Y = finalHeight + 66;
            panel.AddChild(closeButton);
            closeButton.Click += (sender, args) =>
            {
                chartWindow.RemoveFromRoot();
            };

            chartWindow.AddToRoot();
            _chartWindows.Add(chartWindow);
        }
    }

    private void CreateStartPanel()
    {
        //Creating the start panel that will hold all of the buttons for the UI
        StackPanel StartPanel = new StackPanel();
        StartPanel.Spacing = 5;
        StartPanel.Anchor(Anchor.TopLeft);
        StartPanel.AddToRoot();

        //Create Save Button:
        Button SaveButton = new Button();
        SaveButton.Text = "Save";

        StartPanel.AddChild(SaveButton); // Add the Save button as a child to the StartPanel

        SaveButton.Click += (sender, args) =>
        {
            OpenSaveWindow();
        };

        //Add button 2
        Button LoadButton = new Button();
        LoadButton.Text = "Load";
        StartPanel.AddChild(LoadButton);

        LoadButton.Click += (sender, args) =>
        {
            OpenLoadWindow();
        };

        //Add button 3 -> ExitButton
        Button ExitButton = new Button();
        ExitButton.Text = "Exit";
        StartPanel.AddChild(ExitButton);

        ExitButton.Click += (sender, args) =>
        {
            Console.WriteLine("Clicked on the exit button!");
            Environment.Exit(0);
        };

        //Add Road Path Button:
        Button RoadPathButton = new Button();
        RoadPathButton.Text = "Draw Road False";
        StartPanel.AddChild(RoadPathButton);

        RoadPathButton.Click += (sender, args) =>
        {
            Console.WriteLine("Clicked on the road path button!");
            // Logic to enable road path editing mode goes here
            RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
            roadMesh.CreateEnabled = !roadMesh.CreateEnabled;
            RoadPathButton.Text = "Draw Road " + roadMesh.CreateEnabled;

        };

        Button PathTypeButton = new Button();
        PathTypeButton.Text = "Path Type: Straight";
        StartPanel.AddChild(PathTypeButton);

        PathTypeButton.Click += (sender, args) =>
        {
            RoadMesh roadMesh = ComponentManager.GetGlobalComponent<RoadMesh>();
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
            SimulationSystems.Clear();
        };
        
        Button ClearCarsButton = new Button();
        ClearCarsButton.Text = "Clear Cars";
        StartPanel.AddChild(ClearCarsButton);
        ClearCarsButton.Click += (sender, args) =>
        {
            SimulationSystems.DestroyCars();
        };



        //Add Button to run simulation :
        Button runButton = new Button();
        runButton.Text = "Run simulation";
        StartPanel.AddChild(runButton);

        runButton.Click += (sender, args) =>
        {
            OpenRunSimulationWindow();
        };

        ColoredRectangleRuntime SpeedRectangle = new ColoredRectangleRuntime();
        SpeedRectangle.Color = Microsoft.Xna.Framework.Color.DarkGray;
        SpeedRectangle.Dock(Dock.SizeToChildren);
        // Small padding around the content.
        SpeedRectangle.Width = 16;
        SpeedRectangle.Height = 16;
        StartPanel.AddChild(SpeedRectangle);

        StackPanel RectangleStackPanel = new StackPanel();
        RectangleStackPanel.Spacing = 4;    
        RectangleStackPanel.Dock(Dock.SizeToChildren);
        SpeedRectangle.AddChild(RectangleStackPanel);

        Label SpeedLabel = new Label();
        SpeedLabel.Text = " Simulation \n Speed: ";
        RectangleStackPanel.AddChild(SpeedLabel);

        StackPanel SpeedPanel = new StackPanel();
        SpeedPanel.Spacing = 4;
        RectangleStackPanel.AddChild(SpeedPanel);
        SpeedPanel.Orientation = Orientation.Horizontal;
        int lastNonZeroSpeed = 1;
        bool isPaused = false;

        Button PauseButton = new Button();

        void SetSimSpeedFromUi(int speed)
        {
            SimulationSystems.SetSpeed(speed);

            if (speed > 0)
            {
                lastNonZeroSpeed = speed;
                isPaused = false;
                PauseButton.Text = "||";
            }
            else
            {
                isPaused = true;
                PauseButton.Text = "|>";
            }
        }

        Button Speed1Button = new Button();
        Speed1Button.Text = "1x";
        Speed1Button.Width = 30;
        Speed1Button.Height = 15;
        SpeedPanel.AddChild(Speed1Button);
        Speed1Button.Click += (sender, args) =>
        {
            SetSimSpeedFromUi(1);
        };

        Button Speed2Button = new Button();
        Speed2Button.Text = "5x";
        Speed2Button.Width = 30;
        Speed2Button.Height = 15;
        SpeedPanel.AddChild(Speed2Button);
        Speed2Button.Click += (sender, args) =>
        {
            SetSimSpeedFromUi(5);
        };

        Button Speed4Button = new Button();
        Speed4Button.Text = "10x";
        Speed4Button.Width = 30;
        Speed4Button.Height = 15;
        SpeedPanel.AddChild(Speed4Button);
        Speed4Button.Click += (sender, args) =>
        {
            SetSimSpeedFromUi(10);
        };

        PauseButton.Text = "||";
        PauseButton.Width = 45;
        PauseButton.Height = 15;
        SpeedPanel.AddChild(PauseButton);
        PauseButton.Click += (sender, args) =>
        {
            if (isPaused)
            {
                SetSimSpeedFromUi(lastNonZeroSpeed);
            }
            else
            {
                SetSimSpeedFromUi(0);
            }
        };

        //Weather Button Doesn't do anything right now, maybe we'll add some later or delete it.
        Button WeatherButton = new Button();
        WeatherButton.Text = "Weather options";
        //StartPanel.AddChild(WeatherButton);

        WeatherButton.Click += (sender, args) =>
        {
            Window WeatherWindow = CreateWindow();
            WeatherWindow.AddToRoot();
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

            //Button to Add A Sign: Also doesn't do anything right now, but we can add functionality later or delete it.
            Button AddSignButton = new Button();
            AddSignButton.Text = "Add Sign";
            //StartPanel.AddChild(AddSignButton);

            AddSignButton.Click += (sender, args) =>
            {
                Console.WriteLine("Clicked on the add sign button!");
                // Logic to enable adding a sign goes here
            };
        };
    }

    private void OpenSaveWindow()
    {
        if (_saveWindow == null)
        {
            _saveWindow = CreateWindow();
            _saveWindow.Width = 420;
            _saveWindow.Height = 360;

            StackPanel savePanel = new StackPanel();
            savePanel.Spacing = 3;
            savePanel.Anchor(Anchor.Center);
            _saveWindow.AddChild(savePanel);

            Label saveLabel = new Label();
            saveLabel.Text = "File Name: ";
            savePanel.AddChild(saveLabel);

            _saveInputBox = new TextBox();
            savePanel.AddChild(_saveInputBox);

            Label fileLabel = new Label();
            fileLabel.Text = "Select file to save";
            savePanel.AddChild(fileLabel);

            _saveListBox = new ListBox();
            _saveListBox.Width = 260;
            _saveListBox.Height = 160;
            savePanel.AddChild(_saveListBox);
            _saveListBox.ItemClicked += (sender, args) =>
            {
                if (_saveListBox.SelectedObject != null)
                {
                    _saveInputBox.Text = _saveListBox.SelectedObject.ToString();
                }
            };

            Button submitButton = new Button();
            submitButton.Text = "Save File";
            savePanel.AddChild(submitButton);
            submitButton.Click += (sender, args) =>
            {
                if (_saveInputBox.Text == "")
                    return;

                SaveSystem.Save("saves/" + _saveInputBox.Text + ".ism");
                _saveWindow.RemoveFromRoot();
            };

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            savePanel.AddChild(cancelButton);
            cancelButton.Click += (sender, args) =>
            {
                _saveWindow.RemoveFromRoot();
            };
        }

        RefreshSaveFiles(_saveListBox);
        _saveInputBox.Text = "";
        _saveWindow.RemoveFromRoot();
        _saveWindow.AddToRoot();
    }

    private void OpenLoadWindow()
    {
        if (_loadWindow == null)
        {
            _loadWindow = CreateWindow();
            _loadWindow.Width = 420;
            _loadWindow.Height = 320;

            StackPanel loadPanel = new StackPanel();
            loadPanel.Anchor(Anchor.Center);
            loadPanel.Spacing = 4;
            _loadWindow.AddChild(loadPanel);

            Label loadLabel = new Label();
            loadLabel.Text = "Select which road system to open";
            loadPanel.AddChild(loadLabel);

            _loadListBox = new ListBox();
            _loadListBox.Width = 260;
            _loadListBox.Height = 160;
            loadPanel.AddChild(_loadListBox);

            Button loadButton = new Button();
            loadButton.Text = "Load";
            loadPanel.AddChild(loadButton);
            loadButton.Click += (sender, args) =>
            {
                if (_loadListBox.SelectedObject == null)
                    return;

                LoadSystem.Load("saves/" + _loadListBox.SelectedObject + ".ism");
                _loadWindow.RemoveFromRoot();
            };

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            loadPanel.AddChild(cancelButton);
            cancelButton.Click += (sender, args) =>
            {
                _loadWindow.RemoveFromRoot();
            };
        }

        RefreshSaveFiles(_loadListBox);
        _loadWindow.RemoveFromRoot();
        _loadWindow.AddToRoot();
    }

    private void OpenRunSimulationWindow()
    {
        if (ComponentManager.GetCount<PathSegment>() < 4)
        {
            Console.WriteLine("Need at least 4 path segments to run simulation.");
            return;
        }

        if (_runSimulationWindow == null)
        {
            _runSimulationWindow = CreateWindow();
            _runSimulationWindow.Width = 440;
            _runSimulationWindow.Height = 430;

            StackPanel spawnPanel = new StackPanel();
            spawnPanel.Spacing = 4;
            spawnPanel.Anchor(Anchor.Center);
            _runSimulationWindow.AddChild(spawnPanel);

            Label carCountLabel = new Label();
            carCountLabel.Text = "Total cars";
            spawnPanel.AddChild(carCountLabel);
            _carCountTextbox = new TextBox();
            spawnPanel.AddChild(_carCountTextbox);

            Label percentLabel = new Label();
            percentLabel.Text = "Percent basic (0-100)";
            spawnPanel.AddChild(percentLabel);
            _percentTextbox = new TextBox();
            spawnPanel.AddChild(_percentTextbox);

            Label seedLabel = new Label();
            seedLabel.Text = "Seed";
            spawnPanel.AddChild(seedLabel);
            _seedTextbox = new TextBox();
            spawnPanel.AddChild(_seedTextbox);

            Label maxTimeLabel = new Label();
            maxTimeLabel.Text = "Timer per simulation run (seconds)";
            spawnPanel.AddChild(maxTimeLabel);
            _maxSimTimeTextbox = new TextBox();
            spawnPanel.AddChild(_maxSimTimeTextbox);

            Label distributionStepLabel = new Label();
            distributionStepLabel.Text = "Distribution step";
            spawnPanel.AddChild(distributionStepLabel);
            _distributionStepTextbox = new TextBox();
            spawnPanel.AddChild(_distributionStepTextbox);

            _runSimulationStatusLabel = new Label();
            _runSimulationStatusLabel.Text = "";
            spawnPanel.AddChild(_runSimulationStatusLabel);

            Button startSimButton = new Button();
            startSimButton.Text = "Start Simulation";
            spawnPanel.AddChild(startSimButton);
            startSimButton.Click += (sender, args) =>
            {
                SimulationSuper simSuper = ComponentManager.GetGlobalComponent<SimulationSuper>();
                int carCount;
                int seed = 0;
                float behaviorDistribution = 0;
                float maxSimTime = 0;
                int distributionStep = 0;

                bool validInput =
                    int.TryParse(_carCountTextbox.Text, out carCount) &&
                    int.TryParse(_seedTextbox.Text, out seed) &&
                    float.TryParse(_percentTextbox.Text, out behaviorDistribution) &&
                    float.TryParse(_maxSimTimeTextbox.Text, out maxSimTime) &&
                    int.TryParse(_distributionStepTextbox.Text, out distributionStep) &&
                    carCount > 0 &&
                    distributionStep > 0 &&
                    behaviorDistribution >= 0 &&
                    behaviorDistribution <= 100 &&
                    maxSimTime > 0;

                if (!validInput)
                {
                    _runSimulationStatusLabel.Text = "Invalid values. Check fields and try again.";
                    return;
                }

                simSuper.GenCarCount = carCount;
                simSuper.GenSeed = seed;
                simSuper.GenBehaviorDistribution = behaviorDistribution;
                simSuper.MaxSimTime = maxSimTime;
                simSuper.DistributionChangeValue = distributionStep;

                bool started = SimulationSystems.StartBatch();
                if (started)
                {
                    _runSimulationStatusLabel.Text = "";
                    _runSimulationWindow.RemoveFromRoot();
                }
                else
                {
                    _runSimulationStatusLabel.Text = "Need at least 4 path segments.";
                }
            };

            Button exitSpawnButton = new Button();
            exitSpawnButton.Text = "Cancel";
            spawnPanel.AddChild(exitSpawnButton);
            exitSpawnButton.Click += (sender, args) =>
            {
                _runSimulationWindow.RemoveFromRoot();
            };
        }

        SimulationSuper currentSettings = ComponentManager.GetGlobalComponent<SimulationSuper>();
        _carCountTextbox.Text = currentSettings.GenCarCount.ToString();
        _percentTextbox.Text = currentSettings.GenBehaviorDistribution.ToString();
        _seedTextbox.Text = currentSettings.GenSeed.ToString();
        _maxSimTimeTextbox.Text = currentSettings.MaxSimTime.ToString();
        _distributionStepTextbox.Text = currentSettings.DistributionChangeValue.ToString();
        _runSimulationStatusLabel.Text = "";
        _runSimulationWindow.RemoveFromRoot();
        _runSimulationWindow.AddToRoot();
    }

    private void RefreshSaveFiles(ListBox targetList)
    {
        if (!Directory.Exists("saves"))
        {
            Directory.CreateDirectory("saves");
        }

        targetList.Items.Clear();
        foreach (string fileName in Directory.EnumerateFiles("saves"))
        {
            targetList.Items.Add(Path.GetFileNameWithoutExtension(fileName));
        }
    }

    private Window CreateWindow()
    {
        Window NewWindow = new Window();
        NewWindow.Anchor(Anchor.Center);
        NewWindow.Width = 400;
        NewWindow.Height = 300;

        //Create a TextBox to display some information in the window:
        // TextBox windowBox = new TextBox();
        //windowBox.IsReadOnly = true; 
        //windowBox.Text = "This is a window for Graphs!";

        //Button To close the window:
        //Button CloseWindowButton = new Button();
        //CloseWindowButton.Anchor(Anchor.TopLeft);
        //CloseWindowButton.Text = "Exit";
        //NewWindow.AddChild(CloseWindowButton);

        //CloseWindowButton.Click += (sender, args) =>
        //{
        //    NewWindow.RemoveFromRoot(); 
        //};

        return NewWindow;

    }

    private ItemsControl CreateFormatSelection()
    {
        ItemsControl NewControl = new ItemsControl();
        NewControl.AddToRoot();

        // Create Buttons to select file type:
        Button PdfButton = new Button();
        PdfButton.Text = "Save as Pdf";
        PdfButton.AddToRoot();
        NewControl.AddChild(PdfButton);

        Button Save1 = new Button();
        Save1.Text = "Save As...";
        NewControl.AddChild(Save1);

        Button Save2 = new Button();
        Save2.Text = "Save As...";
        NewControl.AddChild(Save2);

        Button Save3 = new Button();
        Save3.Text = "Save As...";
        NewControl.AddChild(Save3);

        //Create Exit Button
        Button ExitButton = new Button();
        ExitButton.Text = "Cancel";
        NewControl.AddChild(ExitButton);

        ExitButton.Click += (sender, args) =>
            {
                NewControl.RemoveFromRoot();
            };

        return NewControl;

    }

    private ItemsControl CreateSpawnSelection()
    {
        ItemsControl newControl = new ItemsControl();

        newControl.AddToRoot();

        // Create label for spawn Cars:
        Label SpawnLabel = new Label();
        SpawnLabel.Text = "Spawn Cars";
        newControl.AddChild(SpawnLabel);

        // Create TextBox for spawn count:
        TextBox SpawnCountTextBox = new TextBox();
        newControl.AddChild(SpawnCountTextBox);

        // Create TextBox for spawn seed:
        TextBox SpawnSeedTextBox = new TextBox();
        newControl.AddChild(SpawnSeedTextBox);

        //Create Spawn Button:
        Button SpawnButton = new Button();
        SpawnButton.Text = "Spawn";

        newControl.AddChild(SpawnButton);

        SpawnButton.Click += (sender, args) =>
        {
            int spawnCount;
            int spawnSeed;

            if (int.TryParse(SpawnCountTextBox.Text, out spawnCount) && int.TryParse(SpawnSeedTextBox.Text, out spawnSeed))
            {
                Console.WriteLine($"Spawning {spawnCount} cars with seed {spawnSeed}");
                // Call your car spawning logic here using spawnCount and spawnSeed
                //CarSystems.GenerateRandomCar();
            }
            else
            {
                Console.WriteLine("Invalid input for spawn count or seed. Please enter valid integers.");
            }
        };

        return newControl;
    }

    private Slider CreateSlider()
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



    /* 
     //Add a colored rectangle
            ColoredRectangleRuntime coloredRectangle = new ColoredRectangleRuntime();
            coloredRectangle.Color = Microsoft.Xna.Framework.Color.Red;
            coloredRectangle.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            StartPanel.AddChild(coloredRectangle);

                //Add spawncontainer inside the colored rectangle
                StackPanel SpawnContainer = new StackPanel();
                coloredRectangle.AddChild(SpawnContainer);
            
                    //Add spawn label inside of the spawn container
                    Label SpawnLabel = new Label();
                    SpawnLabel.Text = "Spawn Cars"; 
                    SpawnContainer.AddChild(SpawnLabel);

                    //add Spawn textbox inside of the Spawn container
                    TextBox textBox = new TextBox();
                    SpawnContainer.AddChild(textBox);
*/

    /*    //Button for different save formats
                Button SaveFormatButton = new Button();
                SaveFormatButton.Text = "Save Graph";
                StartPanel.AddChild(SaveFormatButton);

                SaveFormatButton.Click += (sender, args) =>
                { 
                   ItemsControl ControlBox = CreateFormatSelection();
                   StartPanel.AddChild(ControlBox);

                };
    */
}