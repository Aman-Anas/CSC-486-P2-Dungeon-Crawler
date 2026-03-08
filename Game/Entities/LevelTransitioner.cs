using System;
using Godot;

namespace Game.Entities;

[GlobalClass]
public partial class LevelTransitioner : Area3D
{
    [Export(PropertyHint.File)]
    public string NextScene { get; set; } = "";

    public override void _Ready()
    {
        BodyEntered += CheckPlayer;
    }

    private void CheckPlayer(Node3D body)
    {
        if (body is Farmer)
        {
            CallDeferred(MethodName.ChangeScene);
        }
    }

    public void ChangeScene()
    {
        GetTree().ChangeSceneToFile(NextScene);
    }
}
