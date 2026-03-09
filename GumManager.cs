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

            //Add Slider Button:
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
}