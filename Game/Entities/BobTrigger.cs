using System;
using Game.Entities;
using Godot;

namespace Game.Entities;

public partial class BobTrigger : Area3D
{
    [Export(PropertyHint.MultilineText)]
    public string Dialogue { get; set; } = "";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        this.BodyEntered += DetectBob;
    }

    private void DetectBob(Node3D body)
    {
        // How long to take to reveal each char (in ms)
        const int CharRevealTime = 10; // ms

        if (body is NewBob Bob && (Bob.SpeechLabel.Text != Dialogue))
        {
            Bob.SpeechLabel.Text = Dialogue;
            var typewriteTime = CharRevealTime * Dialogue.Length;

            var newTween = GetTree().CreateTween();
            newTween.TweenMethod(
                Callable.From(
                    (float amt) =>
                    {
                        if (GodotObject.IsInstanceValid(Bob))
                            Bob.SpeechLabel.VisibleRatio = amt;
                    }
                ),
                0f,
                1f,
                typewriteTime / 1000f
            );
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.

    public override void _Process(double delta) { }
}
