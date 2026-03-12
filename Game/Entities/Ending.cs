using System;
using Game;
using Game.Entities;
using Godot;
using GodotTask;

public partial class Ending : Node3D
{
    [Export]
    Control GoodEndingRoot = null!;

    [Export]
    Control BadEndingRoot = null!;

    [Export]
    Label GoodDialogue = null!;

    [Export]
    Label BadDialogue = null!;

    [Export]
    Label timeInfo = null!;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        double time = (Time.GetTicksMsec() - Manager.Instance.Data.TimeGameStarted) / 1000.0;
        timeInfo.Text = $"Your time: {time:F3}s";

        if (Manager.Instance.Data.BurgersEaten > 0)
        {
            GoodEndingRoot.Hide();
            BadEndingRoot.Show();
            SpawnEnemies().Forget();
        }
        else
        {
            GoodEndingRoot.Show();
            BadEndingRoot.Hide();
        }

        var tween = GetTree().CreateTween();

        GoodDialogue.VisibleRatio = 0;
        BadDialogue.VisibleRatio = 0;
        tween.TweenProperty(GoodDialogue, "visible_ratio", 1.0, 1.5);
        tween.TweenProperty(BadDialogue, "visible_ratio", 1.0, 1.5);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }

    [Export]
    PackedScene burgerScene = null!;

    [Export]
    Farmer farmer = null!;

    [Export]
    Node3D spawnBurgerPoint = null!;

    public async GDTaskVoid SpawnEnemies()
    {
        while (GodotObject.IsInstanceValid(this) && GodotObject.IsInstanceValid(farmer))
        {
            CallDeferred(MethodName.SpawnBurger);
            await GDTask.Delay(TimeSpan.FromSeconds(3));
        }
    }

    public void SpawnBurger()
    {
        var newBurger = burgerScene.Instantiate<NewBurger>();
        GetTree().Root.AddChild(newBurger);

        newBurger.PlayerProp = farmer;
        newBurger.GlobalPosition = spawnBurgerPoint.GlobalPosition;
    }
}
