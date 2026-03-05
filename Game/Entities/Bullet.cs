using Godot;
using System;

public partial class Bullet : RigidBody3D
{
    public void Kill()
    {
        this.QueueFree();
    }
}
