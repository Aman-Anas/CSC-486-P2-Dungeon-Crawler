using System;
using Godot;

namespace Game.Entities;

public partial class GravityArea : Area3D
{
    [Export]
    public Vector3 LocalDirection { get; set; } = Vector3.Down;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GravityDirection = GlobalBasis * LocalDirection;
    }
}
