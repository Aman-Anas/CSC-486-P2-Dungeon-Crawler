using Godot;
using System;

public partial class Bullet : RigidBody3D
{
    
    public void Kill()
    {
        this.QueueFree();
    }
    
    public Bullet SetDamageAmount(int amount)
    {
        DamageManager.SetDamage(this, amount);
        return this;
    }
    
    public Bullet SetDamageAppliesTo(String force)
    {
        DamageManager.SetDamageApplyTo(this, force);
        return this;
    }
    
}
