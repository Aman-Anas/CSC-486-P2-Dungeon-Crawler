using Godot;

namespace Game.Entities;

public partial class NewBob : RigidBody3D
{
    [Export]
    Farmer? playerToFollow;

    [Export]
    AnimationPlayer animPlayer = null!;

    [Export]
    float minimumFollowingDistance = 3.0f;

    [Export]
    float followSpeed = 5.0f;

    [Export]
    StringName runAnimation = "UAL1_Standard/Sprint";

    [Export]
    StringName idleAnimation = "UAL1_Standard/Idle";

    [Export]
    float animationBlendAmount = 0.5f;

    public override void _Ready() { }

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

        // Make Bob look at the player
        this.LookAt(playerPos, Vector3.Up);

        var distanceToPlayer = myPos.DistanceTo(playerPos);

        // If we're close enough to the player, just do nothing
        if (distanceToPlayer <= minimumFollowingDistance)
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
