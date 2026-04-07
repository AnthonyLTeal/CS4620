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

namespace CS4620IS;

class GumInterface
{
    public void InitializeUI()
    {
        GumService.Default.Root.Children.Clear();
        CreateStartPanel();
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
                Console.WriteLine("Clicked on the Save button!");
                Window SaveWindow = CreateWindow();
                SaveWindow.Height = 400;
                SaveWindow.AddToRoot();
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
                        SaveBox.Items.Add("New File");
                       // foreach (String file in Directory.GetFiles(@"CRoadSaves"))
                        //{
                        //    String InputItem = (file);
                        //    SaveBox.Items.Add(InputItem);
                        //}
            

                        // Add Button to Save Road Data:
                        Button SButton = new Button();
                        SButton.Text = "Save File";
                        //SButton.Anchor(Anchor.Center);
                        SavePanel.AddChild(SButton);

                        SButton.Click += (sender, args) =>
                        {
                             if(inputBox.Text == "")
                                {
                                Console.WriteLine("Please Enter a file name first");
                                }

                            else{    
                                // LoadSystem.Load(SaveBox.SelectedIndex);
                                SaveWindow.RemoveFromRoot();
                                }
                        };

                        // Add Cancel Button to the Panel:
                        Button CloseWindowButton = new Button();
                        //CloseWindowButton.Anchor(Anchor.Bottom);
                        //CloseWindowButton.Anchor(Anchor.Center);
                        CloseWindowButton.Text = "Cancel";
                        SavePanel.AddChild(CloseWindowButton);
            
                        CloseWindowButton.Click += (sender, args) =>
                        {
                            SaveWindow.RemoveFromRoot(); 
                        };             

                            


                    
            };

            //Add button 2
            Button LoadButton = new Button();
            LoadButton.Text = "Load";  
            StartPanel.AddChild(LoadButton);

            LoadButton.Click += (sender, args) =>
            {
                Console.WriteLine("Clicked on the load button!");
                Window LoadWindow = CreateWindow();
                LoadWindow.Height = 350;
                LoadWindow.AddToRoot();


                    StackPanel LoadPanel = new StackPanel();
                    LoadPanel.Anchor(Anchor.Center);
                    LoadPanel.Spacing = 4;
                    LoadWindow.AddChild(LoadPanel);

                        Label LoadLabel = new Label();
                        LoadLabel.Text = "Select which road system to open";
                        LoadPanel.AddChild(LoadLabel);

                        //Creating a List Box to view files
                        ListBox LoadBox = new ListBox();
                        LoadPanel.AddChild(LoadBox);
                        for(int i = 0; i < 20; i++)
                        {
                            String InputItem = ("File");
                            LoadBox.Items.Add(InputItem);
                        }

                        //Create a Button to load once file is selected from listBox:
                        Button LButton = new Button();
                        LButton.Text = "Load";
                        LoadPanel.AddChild(LButton);
                        LButton.Click += (sender, args) =>
                        {
                            // LoadSystem.Load(ListBox.SelectedStateName);
                            LoadWindow.RemoveFromRoot();
                        };


                        Button CancelButton = new Button();
                        CancelButton.Text = "Cancel";
                        LoadPanel.AddChild(CancelButton);

                        CancelButton.Click += (sender, args) =>
                        {
                            LoadWindow.RemoveFromRoot();
                        };
            
                    

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
            RoadPathButton.Text = "Draw Road";  
            StartPanel.AddChild(RoadPathButton);

            RoadPathButton.Click += (sender, args) =>
            {
                Console.WriteLine("Clicked on the road path button!");
                // Logic to enable road path editing mode goes here
                
            };

            //Add Graph Window Button:
            Button GraphWindowButton = new Button();
            GraphWindowButton.Text = "View Charts";
            StartPanel.AddChild(GraphWindowButton);

            GraphWindowButton.Click += (sender, args) =>
            {
                Window GraphWindow = CreateWindow();
                GraphWindow.AddToRoot();

                StackPanel ChartPanel = new StackPanel();
                ChartPanel.Spacing = 20;
                ChartPanel.Anchor(Anchor.Center);   
                GraphWindow.AddChild(ChartPanel);

                Button Chart1Button = new Button();
                Chart1Button.Text = "Chart 1";
                ChartPanel.AddChild(Chart1Button); 

                Button Chart2Button = new Button();
                Chart2Button.Text = "Chart 2";  
                ChartPanel.AddChild(Chart2Button);


                // Add Save format Selection to the graph window:
                Button SaveFormatButton = new Button();
                SaveFormatButton.Text = "Save Graph";
                SaveFormatButton.Anchor(Anchor.BottomRight);
                GraphWindow.AddChild(SaveFormatButton);

                SaveFormatButton.Click += (sender, args) =>
                { 
                    ItemsControl ControlBox = CreateFormatSelection();
                    GraphWindow.AddChild(ControlBox);
                    ControlBox.Anchor(Anchor.BottomRight);

                };

                //Add Exit button to the graph window:
                Button ExitGraphButton = new Button();              
                ExitGraphButton.Text = "Cancel";
                ExitGraphButton.Anchor(Anchor.TopRight);
                ChartPanel.AddChild(ExitGraphButton);

                ExitGraphButton.Click += (sender, args) =>
                {
                    GraphWindow.RemoveFromRoot();
                };

            };

            //Add Weather options button(opens window):
            
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
                Slider RainSlider = CreateSlider();;
                WeatherPanel.AddChild(RainSlider);

                //Create Slider for Snow Intensity:
                WeatherPanel.AddChild(new Label { Text = "Snow Intensity" });
                Slider SnowSlider = CreateSlider();
                WeatherPanel.AddChild(SnowSlider);
            };

            //Button to Add A Sign: 
            Button AddSignButton = new Button();
            AddSignButton.Text = "Add Sign";    
            //StartPanel.AddChild(AddSignButton);

            AddSignButton.Click += (sender, args) =>
            {
                Console.WriteLine("Clicked on the add sign button!");
                // Logic to enable adding a sign goes here
            };

            //Add Spawn Cars Button:
            Button runButton = new Button();
            runButton.Text = "Run simulation";
            StartPanel.AddChild(runButton);

            runButton.Click += (sender, args) => 
            {   
                //Add Window for controls
                Window SpawnWindow = CreateWindow();
                SpawnWindow.AddToRoot();
                
                    StackPanel SpawnPanel = new StackPanel();
                    SpawnPanel.Spacing = 4;
                    SpawnWindow.AddChild(SpawnPanel);

                    //Create Labels and TextBoxes for Static and Dynamic Cars:
                    Label StaticAgentLabel = new Label();
                    StaticAgentLabel.Text = "# of static Cars";
                    SpawnPanel.AddChild(StaticAgentLabel);
                    TextBox StaticAgentTextBox = new TextBox();
                    SpawnPanel.AddChild(StaticAgentTextBox);

                    Label DynamicAgentLabel = new Label();
                    DynamicAgentLabel.Text = "# of rerouting Cars";
                    SpawnPanel.AddChild(DynamicAgentLabel);
                    TextBox DynamicAgentTextBox = new TextBox();
                    SpawnPanel.AddChild(DynamicAgentTextBox);

                    //Create Button to Start Simulation:
                    Button StartSimButton = new Button();   
                    StartSimButton.Text = "Start Simulation";
                    SpawnPanel.AddChild(StartSimButton);    

                    StartSimButton.Click += (sender, args) =>
                    {
                        int staticCount;
                        int dynamicCount;

                        if (int.TryParse(StaticAgentTextBox.Text, out staticCount) && int.TryParse(DynamicAgentTextBox.Text, out dynamicCount))
                        {
                            Console.WriteLine($"Spawning {staticCount} static cars and {dynamicCount} dynamic cars");
                            // Call your car spawning logic here using staticCount and dynamicCount
                            CarSystems.GenerateRandomCar();
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
                        SpawnWindow.RemoveFromRoot();
                    };


            };
            
            
            
       
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
            PdfButton.Text="Save as Pdf";
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
                CarSystems.GenerateRandomCar();
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
