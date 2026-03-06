using System;
using System.Linq;
using Godot;

namespace Game.Entities;

public partial class DualDoor : Node3D
{
    [Export]
    AnimationPlayer player = null!;

    [Export]
    StringName successAnim = "Open";

    [Export]
    Area3D buttonOne = null!;

    [Export]
    Area3D buttonTwo = null!;

    bool opened = false;

    public override void _Ready()
    {
        buttonOne.BodyEntered += Check;
        buttonTwo.BodyEntered += Check;
    }

    private void Check(Node3D _)
    {
        if (opened)
            return;

        bool b1Pressed = buttonOne
            .GetOverlappingBodies()
            .Where(static (body) => body is Farmer or NewBob)
            .Any();

        bool b2Pressed = buttonTwo
            .GetOverlappingBodies()
            .Where(static (body) => body is Farmer or NewBob)
            .Any();

        if (b1Pressed && b2Pressed)
        {
            opened = true;
            player.Play(successAnim);
        }
    }
}
