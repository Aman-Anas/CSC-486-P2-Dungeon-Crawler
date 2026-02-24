using System;
using Godot;

namespace Game.UI;

public partial class FancyProgressBar : TextureProgressBar
{
    [Export]
    Label percentLabel = null!;

    public void SetCoolValue(int value)
    {
        this.Value = value;
        percentLabel.Text = $"{value}";
    }
}
