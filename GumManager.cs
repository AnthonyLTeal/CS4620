using MonoGameGum;
using GumDataTypes;
using GumRuntime;
using Gum.Forms.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
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

        StartButton.Click += (sender, args) =>
        {
            Console.WriteLine("Clicked on the button!");
        };

    }

}