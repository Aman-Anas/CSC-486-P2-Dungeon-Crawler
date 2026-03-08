using System;
using Godot;

namespace Game.UI;

[GlobalClass]
public partial class FancyProgressBar : TextureProgressBar
{
    [Export]
    Label percentLabel = null!;

    public void SetCoolValue(int value)
    {
        this.Value = value;
        this.SetLabelValue($"{value}");
        //percentLabel.Text = $"{value}";
    }
    
    public void SetLabelValue(String value)
    {
        percentLabel.Text = value;
    }
}
