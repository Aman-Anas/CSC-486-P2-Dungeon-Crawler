using System;
using Game.Entities;
using Godot;

namespace Game.Entities;

public partial class BobTrigger : Area3D
{
    [Export]
    public string Dialogue { get; set; } = "";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.BodyEntered += DetectBob;
    }

    private void DetectBob(Node3D body)
    {
        if (body is NewBob Bob)
        {
            Bob.SpeechLabel.Text = Dialogue;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta) { }
}
