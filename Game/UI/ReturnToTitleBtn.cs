using System;
using Godot;

namespace Game.UI;

public partial class ReturnToTitleBtn : Button
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.Pressed += () =>
        {
            Manager.Instance.ExitToTitle();
        };
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
