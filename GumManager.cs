using MonoGameGum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using System;
using System.Collections;
using Gum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using System.ComponentModel;
using Gum.Forms;

class GumInterface
{
    public void InitializeUI()
    {
        GumService.Default.Root.Children.Clear();
        CreateStartPanel();
    }

    private void CreateStartPanel()
    {
        StackPanel StartPanel = new StackPanel();
        StartPanel.Spacing = 5;
        StartPanel.Anchor(Anchor.TopLeft);
        StartPanel.AddToRoot();
        // StartPanel.Dock(Dock.Fill);

            //Add button 1
            Button StartButton = new Button();
            StartButton.Text = "Save";
            //StartButton.Anchor(Anchor.TopLeft);
            StartPanel.AddChild(StartButton);

            StartButton.Click += (sender, args) =>
            {
                Console.WriteLine("Clicked on the button!");
            };

            //Add button 2
            Button SecondButton = new Button();
            SecondButton.Text = "Load";  
            StartPanel.AddChild(SecondButton);

            SecondButton.Click += (sender, args) =>
            {
                Console.WriteLine("Clicked on the second button!");
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
            RoadPathButton.Text = "Road Path";  
            StartPanel.AddChild(RoadPathButton);

            RoadPathButton.Click += (sender, args) =>
            {
                Console.WriteLine("Clicked on the road path button!");
                // Logic to enable road path editing mode goes here
            };

            //Add Graph Window Button:
            Button GraphWindowButton = new Button();
            GraphWindowButton.Text = "Open Graph Window";
            StartPanel.AddChild(GraphWindowButton);

            GraphWindowButton.Click += (sender, args) =>
            {
                CreateWindow();
            };

            //Add Start Simulation Button:
            Button StartSimulationButton = new Button();
            StartSimulationButton.Text = "Start Simulation";
            StartPanel.AddChild(StartSimulationButton);

            StartSimulationButton.Click += (sender, args) =>
            {
                Console.WriteLine("Clicked on the start simulation button!");
                // Logic to start the traffic simulation goes here
            };


            //Add Stop Simulation Button:
            Button StopSimulationButton = new Button();     
            StopSimulationButton.Text = "Stop Simulation";
            StartPanel.AddChild(StopSimulationButton);

            StopSimulationButton.Click += (sender, args) =>
            {
                Console.WriteLine("Clicked on the stop simulation button!");
                // Logic to stop the traffic simulation goes here
            };

            //Add Slider :
            Slider newSlider = new Slider();
            newSlider.AddToRoot();
            newSlider.Maximum = 30;
            newSlider.Minimum = 0;
            newSlider.TicksFrequency = 1;
            newSlider.IsSnapToTickEnabled = true;
            newSlider.Width = 150;
            StartPanel.AddChild(newSlider);

            newSlider.ValueChanged += (sender, args) =>
                {
                    Console.WriteLine($"Slider value changed to: {newSlider.Value}");
                }; 

            Slider newSlider2 = new Slider();
            newSlider2.AddToRoot();
            newSlider2.Maximum = 30;
            newSlider2.Minimum = 0;
            newSlider2.TicksFrequency = 1;
            newSlider2.IsSnapToTickEnabled = true;
            newSlider2.Width = 150;
            StartPanel.AddChild(newSlider2);

            newSlider2.ValueChanged += (sender, args) =>
                {
                    Console.WriteLine($"Slider value changed to: {newSlider.Value}");
                }; 




            

            //Add a colored rectangle
            ColoredRectangleRuntime coloredRectangle = new ColoredRectangleRuntime();
            coloredRectangle.Color = Microsoft.Xna.Framework.Color.MediumAquamarine;
            coloredRectangle.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
            coloredRectangle.Width = -5f;
            StartPanel.AddChild(coloredRectangle);

                //Add spawncontainer inside the colored rectangle
                StackPanel SpawnContainer = new StackPanel();
                coloredRectangle.AddChild(SpawnContainer);
            
                    //Add spawn label inside of the spawn container
                    Label SpawnLabel = new Label();
                    SpawnLabel.Text = "Spawn Cars"; 
                    SpawnContainer.AddChild(SpawnLabel);

                    //add Spawn textbox inside of the Spawn container
                    // Count.input(might be text)
                    TextBox textBox = new TextBox();
                    textBox.Text = "# of cars to spawn";
                    textBox.WidthUnits= Gum.DataTypes.DimensionUnitType.RelativeToParent;
                    textBox.Width = -3f;
                    textBox.Y = 10;
                    SpawnContainer.AddChild(textBox);

                    TextBox SeedTextBox = new TextBox();
                    SeedTextBox.Text = "Seed goes here";
                    SeedTextBox.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
                    SeedTextBox.Width = -3f;
                    SeedTextBox.Y = 25;
                    SpawnContainer.AddChild(SeedTextBox);

        

        

        //Seed.input
        //Count.input(might be text)
    }

    private void CreateWindow()
    {
        Window GraphWindow = new Window();
        GraphWindow.Anchor(Anchor.Center);
        GraphWindow.Width = 400;
        GraphWindow.Height = 300;
        GraphWindow.AddToRoot();
            
            TextBox windowBox = new TextBox();
            windowBox.IsReadOnly = true;
            windowBox.Text = "This is a window for Graphs!";
            GraphWindow.AddChild(windowBox);

            Button CloseWindowButton = new Button();
            CloseWindowButton.Anchor(Anchor.Bottom);
            CloseWindowButton.Text = "Exit";
            GraphWindow.AddChild(CloseWindowButton);
            
                CloseWindowButton.Click += (sender, args) =>
                {
                    GraphWindow.RemoveFromRoot(); 
                };

        
    }

}