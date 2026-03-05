using Game.Entities;
using Godot;
using System;

public partial class BobTrigger : Area3D
{
    
    [Export] public string dialogue; 
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.BodyEntered += detectBob;

    }

    private void detectBob(Node3D body)
    {

        if (body is NewBob Bob) {
            Bob.something.Text = dialogue;
        }

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta)
    {
    }
}
