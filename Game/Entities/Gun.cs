using System;
using Godot;

public partial class Gun : Node3D
{
    [Export]
    Node3D bulletSpawnPoint = null!;

    [Export]
    PackedScene bulletScene = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var newBullet = bulletScene.Instantiate<RigidBody3D>();
        this.AddChild(newBullet);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
