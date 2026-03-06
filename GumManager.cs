using MonoGameGum;
using Gum.Forms.Controls;
using Gum.Wireframe;
using System;

class GumInterface
{
    private void InitializeUI()
    {
        GumService.Default.Root.Children.Clear();
        CreateStartPanel();
    }

    private void CreateStartPanel()
    {
        Panel StartPanel = new Panel();
        StartPanel.AddToRoot();
        StartPanel.Dock(Dock.Fill);

        Button StartButton = new Button();
        StartButton.Anchor(Anchor.TopLeft);
        StartPanel.AddChild(StartButton);

        StartButton.X = 20;
        StartButton.Y = -20;

        StartButton.Click += (sender, args) =>
        {
            Console.WriteLine("Clicked on the button!");
        };

    }

}