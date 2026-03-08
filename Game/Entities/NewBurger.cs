using System;
using Game.UI;
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
    float maxHealth = 100.0f;

    float currentHealth = 0.0f;

    [Export]
    StringName runAnimation = "BAKED_Walk";

    [Export]
    StringName idleAnimation = "RESET";

    [Export]
    StringName disabledAnimation = "BAKED_Activate";

    [Export]
    String disabledText = "Disabled";

    [Export]
    CollisionShape3D? activatedCollisionShape;

    [Export]
    CollisionShape3D? disabledCollisionShape;

    bool disabled = false;

    [Export]
    float animationBlendAmount = 0.5f;

    [Export]
    float InvulnerabilityTime = 0.1f;

    public override void _Ready()
    {
        currentHealth = maxHealth;
        updateHealthBar();
    }

    [Export]
    FancyProgressBar healthBar = null!;

    public void updateHealthBar()
    {
        // scale health to 100
        healthBar.SetCoolValue((int)(currentHealth / maxHealth * 100.0f));
        healthBar.SetLabelValue($"{currentHealth}");
    }

    public void Damage(float amount)
    {
        currentHealth -= amount;
        updateHealthBar();
        if (currentHealth <= 0.0)
            Kill();
    }

    public void Kill()
    {
        currentHealth = 0.0f;
        this.disabled = true;
        this.RemoveMeta(EnemyMeta);
        //this.QueueFree();
    }

    public readonly StringName EnemyMeta = "enemy";
    bool canTakeDamage = true;

    [Export]
    AudioStreamPlayer3D hurtFx = null!;

    public override void _PhysicsProcess(double delta)
    {
        // Figure out if burger is touching a bullet
        var colliding = GetCollidingBodies();
        bool touchingBullet = false;
        int amountToReduceHealth = 0;
        foreach (var collider in colliding)
        {
            if (collider.HasMeta(EnemyMeta))
            {
                touchingBullet = true;

                // Grab the amount to reduce health by
                amountToReduceHealth = (int)collider.GetMeta(EnemyMeta);

                // Kill bullet
                if (collider is Bullet bullet)
                    bullet.Kill();

                break;
            }
        }

        // Take damage
        if (canTakeDamage && touchingBullet)
        {
            // Reduce burger health
            Damage(amountToReduceHealth);

            if (!disabled)
                hurtFx.Play();

            // Start invulnerability timer
            canTakeDamage = false;
            ResetInvulnerability().Forget();
        }
    }

    async GDTaskVoid ResetInvulnerability()
    {
        await GDTask.Delay(TimeSpan.FromSeconds(InvulnerabilityTime));
        canTakeDamage = true;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        if (disabled)
        {
            animPlayer.SpeedScale = -1.0f;
            animPlayer.Play(disabledAnimation);
            healthBar.SetLabelValue(disabledText);

            // apply some rotational friction
            state.AngularVelocity *= 0.9f;

            // apply some translational friction
            var horizontalVelocity = state.LinearVelocity;
            horizontalVelocity.X *= 0.95f;
            horizontalVelocity.Z *= 0.95f;
            state.LinearVelocity = horizontalVelocity;

            // drop to the ground
            activatedCollisionShape.Disabled = true;
            disabledCollisionShape.Disabled = false;

            return;
        }

        if (playerToFollow == null)
        {
            return;
        }

        animPlayer.SpeedScale = 1.0f;

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
