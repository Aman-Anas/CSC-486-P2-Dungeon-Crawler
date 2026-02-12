using System;
using System.Threading.Tasks;
using Game;
using Godot;
using Utilities.Logic;

public partial class MainMenu : Control
{
    // Buttons used in interface
    [Export]
    Button PlayButton = null!;

    [Export]
    Button HelpButton = null!;

    [Export]
    Button QuitButton = null!;

    [Export]
    Button SettingsButton = null!;

    [Export]
    Button HomeButton = null!;

    // The main menu screens
    [Export]
    Control MainMenuRoot = null!;

    [Export]
    Control HelpMenu = null!;

    [Export]
    SettingsMenu SettingsMenu = null!;

    [Export(PropertyHint.File)]
    string gameScene = null!;

    // Helper to manage side menus
    SubMenuHelper mainHelper = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        mainHelper = new(HomeButton, MainMenuRoot);

        HelpMenu.Hide();
        SettingsMenu.Hide();

        PlayButton.Pressed += () => GetTree().ChangeSceneToFile(gameScene);
        HelpButton.Pressed += () => mainHelper.SetSubMenu(HelpMenu);
        SettingsButton.Pressed += () => mainHelper.SetSubMenu(SettingsMenu);
        QuitButton.Pressed += () => GetTree().Quit();

        // If the player closes the menu, we should apply settings.
        mainHelper.OnCloseMenu += (current) =>
        {
            if (current == SettingsMenu)
            {
                SettingsMenu.ApplyAllSettings();
            }
        };
    }
}
