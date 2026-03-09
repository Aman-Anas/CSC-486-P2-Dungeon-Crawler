using System;
using Game.UI;
using Godot;
using GodotTask;

namespace Game.Entities;

public partial class BigBurger : RigidBody3D
{
    [Export]
    Farmer? playerToFollow;

    [Export]
    AnimationPlayer animPlayer = null!;

    [Export]
    float maximumFollowingDistance = 15.0f;

    [Export]
    float minimumFollowingDistance = 1.0f;

    [Export]
    float followSpeed = 1.5f;

    [Export]
    float maxHealth = 200.0f;

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
    CollisionShape3D activatedCollisionShape = null!;

    [Export]
    CollisionShape3D disabledCollisionShape = null!;

    bool disabled = false;

    [Export]
    float animationBlendAmount = 0.5f;

    [Export]
    float InvulnerabilityTime = 0.1f;

    [Export]
    public int BodyDamage = 20;

    [Export]
    public int BulletDamage = 10;

    public override void _Ready()
    {
        currentHealth = maxHealth;
        updateHealthBar();

        // set damage meta
        DamageManager.SetMyForce(this, DamageManager.BurgerForceName);
        DamageManager.SetDamage(this, BodyDamage);
        DamageManager.SetDamageApplyTo(this, DamageManager.FarmerForceName);
    }

    [Export]
    BigBurgerBars bars = null!;

    //FancyProgressBar healthBar = null!;

    public void updateHealthBar()
    {
        // scale health to 100
        bars.SetHealthValue((int)currentHealth);
        //healthBar.SetCoolValue((int)(currentHealth / maxHealth * 100.0f));
        //healthBar.SetLabelValue($"{currentHealth}");
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

    public override void _PhysicsProcess(double delta)
    {
        // Figure out if burger is touching a bullet
        var colliding = GetCollidingBodies();
        bool touchingBullet = false;
        int amountToReduceHealth = 0;
        foreach (var collider in colliding)
        {
            //if (collider.HasMeta(EnemyMeta))
            if (DamageManager.CanDamageMe(this, collider))
            {
                touchingBullet = true;

                // Grab the amount to reduce health by
                amountToReduceHealth = DamageManager.GetDamageAmount(collider);
                //amountToReduceHealth = (int)collider.GetMeta(EnemyMeta);

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
            bars.SetHealthDisplay(disabledText);
            //healthBar.SetLabelValue(disabledText);

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

        animPlayer.SpeedScale = 1.0f * 1.5f / 3.5f;

        var localLinearVelocity = GlobalBasis.Inverse() * state.LinearVelocity;
        localLinearVelocity.X = 0;

        var myPos = GlobalPosition;
        var playerPos = playerToFollow.GlobalPosition with { Y = myPos.Y };

        this.LookAt(playerPos, Vector3.Up);

        // Aim the bullet spawn marker at the player (for visual alignment)
        var markerAimTarget = playerToFollow.GlobalPosition with { Y = playerToFollow.GlobalPosition.Y + 1.5f };
        bulletSpawnPoint.LookAt(markerAimTarget, Vector3.Up);

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

    [Export]
    Node3D bulletSpawnPoint = null!;

    [Export]
    PackedScene bulletScene = null!;

    [Export]
    float bulletSpeed = 45.0f;

    [Export]
    float despawnTime = 10.0f;

    [Export]
    float reloadTime = 0.8f;

    bool readyToFire = true;

    [Export]
    AudioStreamPlayer? shootfx;

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed(GameActions.PlayerFire) && readyToFire && playerToFollow != null)
        {
            var newBullet = bulletScene.Instantiate<RigidBody3D>();
            ((Bullet)newBullet).SetDamageAmount(BulletDamage).SetDamageAppliesTo(DamageManager.FarmerForceName);

            newBullet.GlobalTransform = bulletSpawnPoint.GlobalTransform;
            var aimTarget = playerToFollow.GlobalPosition with { Y = playerToFollow.GlobalPosition.Y + 1.5f };
            newBullet.LookAt(aimTarget, Vector3.Up);
            newBullet.RotateObjectLocal(Vector3.Up, (float)(-Math.PI / 2));
            newBullet.LinearVelocity = -bulletSpawnPoint.GlobalBasis.Z * bulletSpeed;

            GetTree().CurrentScene.AddChild(newBullet);
            shootfx?.Play();

            // Remove the bullet after some time
            var timer = GetTree().CreateTimer(despawnTime);
            timer.Timeout += newBullet.QueueFree;

            readyToFire = false;
            StartReload();
        }
    }

    async void StartReload()
    {
        await GDTask.Delay(TimeSpan.FromSeconds(reloadTime));
        readyToFire = true;
    }
}
