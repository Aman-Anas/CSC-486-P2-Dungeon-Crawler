using Godot;
using GodotTask;

namespace Game.Entities;

public partial class NewBurger : RigidBody3D
{
    [Export]
    Farmer? playerToFollow;

    [Export]
    AnimationPlayer animPlayer = null!;

    [Export]
    float maximumFollowingDistance = 5.0f;

    [Export]
    float minimumFollowingDistance = 1.0f;

    [Export]
    float followSpeed = 5.0f;

    [Export]
    StringName runAnimation = "BAKED_Walk";

    [Export]
    StringName idleAnimation = "RESET";

    [Export]
    float animationBlendAmount = 0.5f;

    public override void _Ready() { }

    public void Kill()
    {
        this.QueueFree();
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (playerToFollow == null)
        {
            return;
        }

        var localLinearVelocity = GlobalBasis.Inverse() * state.LinearVelocity;
        localLinearVelocity.X = 0;

        var myPos = GlobalPosition;
        var playerPos = playerToFollow.GlobalPosition with { Y = myPos.Y };

        this.LookAt(playerPos, Vector3.Up);

        var distanceToPlayer = myPos.DistanceTo(playerPos);

        // If close enough or too far away, do nothing
        if (
            distanceToPlayer <= minimumFollowingDistance
            || distanceToPlayer >= maximumFollowingDistance
        )
        {
            localLinearVelocity.Z = 0;
            animPlayer.Play(idleAnimation, customBlend: animationBlendAmount);
        }
        else
        {
            localLinearVelocity.Z = -followSpeed;
            animPlayer.Play(runAnimation, customBlend: animationBlendAmount);
        }

        state.LinearVelocity = GlobalBasis * localLinearVelocity;
    }
}
