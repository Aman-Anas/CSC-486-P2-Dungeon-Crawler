using Godot;
using System;

namespace Game.UI;

[GlobalClass]
public partial class BigBurgerBars : Control
{
    [Export]
    FancyProgressBar healthBar = null!;
    
    [Export]
    FancyProgressBar reloadBar = null!;
    
    public void SetHealthValue(int value)
    {
        healthBar.SetCoolValue(value);
    }
    
    public void SetReloadValue(float percentage)
    {
        reloadBar.SetCoolValue((int)(percentage * reloadBar.MaxValue));
        reloadBar.SetLabelValue($"{(int)(percentage * 100)}%");
    }
    
    public void SetHealthDisplay(String value)
    {
        healthBar.SetLabelValue(value);
    }
    
    public void SetReloadDisplay(String value)
    {
        reloadBar.SetLabelValue(value);
    }
}
