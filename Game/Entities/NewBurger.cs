using Godot;
using GodotTask;
using Game.UI;

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
    float animationBlendAmount = 0.5f;

    public override void _Ready()
    {
        currentHealth = maxHealth;
        updateHealthBar();
    }

    [Export]
    FancyProgressBar healthBar = null!;

    public void updateHealthBar()
    {
        healthBar.SetCoolValue((int) currentHealth);
    }

    public void Damage(float amount)
    {
        currentHealth -= amount;
        updateHealthBar();
        if (currentHealth <= 0.0) Kill();
    }

    public void Kill()
    {
        this.QueueFree();
    }
    
    public readonly StringName EnemyMeta = "enemy";
    bool canTakeDamage = true;
    
    public override void _PhysicsProcess(double delta)
    {
        //GD.Print("physics");
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
                if (collider is Bullet bullet) bullet.Kill();
                
                break;
            }
        }

        // Take damage
        if (canTakeDamage && touchingBullet)
        {
            // Reduce burger health
            Damage(amountToReduceHealth);
        }
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
