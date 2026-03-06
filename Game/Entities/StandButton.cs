using System;
using Godot;

namespace Game.Entities;

public partial class StandButton : Area3D
{
    [Export]
    public StandButton Other { get; set; } = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        BodyEntered += EnterPlayer;
        BodyExited += ExitPlayer;
    }

    private void ExitPlayer(Node3D body)
    {
        if (body is Farmer farmer)
        {
            farmer.OtherButtonToPress = null;
        }
    }

    private void EnterPlayer(Node3D body)
    {
        if (body is Farmer farmer)
        {
            farmer.OtherButtonToPress = Other;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta) { }
}
