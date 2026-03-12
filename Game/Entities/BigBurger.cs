using System;
using System.Collections.Generic;
using Game.UI;
using Godot;
using GodotTask;

namespace Game.Entities;

public enum BossState
{
    Shooting, // Shoot at player until dash cooldown is ready
    DashWindUp, // Brief pause when dash is charged, no shooting
    Charging, // Charge at player
    DashCooldown // Recovering after charge, then back to Shooting
}

public partial class BigBurger : RigidBody3D
{
    [Export]
    Farmer? playerToFollow;

    [Export]
    AnimationPlayer animPlayer = null!;

    [Export]
    float maximumFollowingDistance = 25.0f;

    [Export]
    float minimumFollowingDistance = 1.0f;

    [Export]
    float followSpeed = 1.5f;

    [Export]
    float maxHealth = 500.0f;

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

    // --- FSM: Shooting -> DashWindUp -> Charging -> DashCooldown -> Shooting ---
    BossState currentState = BossState.Shooting;
    float dashReadinessTimer; // Countdown in Shooting; when 0, transition to DashWindUp
    float dashWindUpTimer; // Brief pause when fully charged before dashing
    float chargeTimer; // Duration of charge
    float dashCooldownTimer; // Recovery time after charge

    [Export]
    float timeBeforeDashAvailable = 5.0f; // Seconds in Shooting before dash is ready

    [Export]
    float dashWindUpDuration = 1.0f; // Seconds to pause (no shooting) before charging

    [Export]
    float chargeDuration = 1.5f; // How long the charge lasts

    [Export]
    float dashCooldownAfterCharge = 2.0f; // Recovery time before shooting again

    [Export]
    float chargeSpeed = 12.0f; // Speed when charging (faster than followSpeed)

    [Export]
    public int BodyDamage = 20;

    [Export]
    public int BulletDamage = 10;

    public override void _Ready()
    {
        currentHealth = maxHealth;
        bars.SetHealthMax(maxHealth);
        updateHealthBar();
        dashReadinessTimer = timeBeforeDashAvailable;
        UpdateCooldownBar();

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
        bars.SetHealthValue((int)currentHealth);
    }

    void UpdateCooldownBar()
    {
        float percentage;
        switch (currentState)
        {
            case BossState.Shooting:
                // Charge up from 0 to 100% as dash readiness builds
                percentage = 1f - (dashReadinessTimer / timeBeforeDashAvailable);
                break;
            case BossState.DashWindUp:
                percentage = 1f; // Fully charged, about to dash
                break;
            case BossState.Charging:
                // Decrease from 100 to 0% while dashing
                percentage = chargeTimer / chargeDuration;
                break;
            default:
                percentage = 0f;
                break;
        }
        bars.SetReloadValue(percentage);
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
        DamageManager.ClearDamageMeta(this);
        foreach (var bullet in spawnedBullets)
        {
            if (GodotObject.IsInstanceValid(bullet))
                bullet.Kill();
        }
        spawnedBullets.Clear();
        CallDeferred(MethodName.SwitchToEndScene);
    }

    public void SwitchToEndScene()
    {
        GetTree().ChangeSceneToFile(NextScene);
    }

    [Export(PropertyHint.File)]
    public string NextScene = "";

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
            return;

        var baseAnimSpeed = 1.0f * 1.5f / 3.5f;
        animPlayer.SpeedScale =
            currentState == BossState.Charging
                ? baseAnimSpeed * (chargeSpeed / followSpeed)
                : baseAnimSpeed;

        var myPos = GlobalPosition;
        var playerPos = playerToFollow.GlobalPosition with { Y = myPos.Y };
        var distanceToPlayer = myPos.DistanceTo(playerPos);

        this.LookAt(playerPos, Vector3.Up);

        // Aim the bullet spawn marker at the player
        var markerAimTarget = playerToFollow.GlobalPosition with
        {
            Y = playerToFollow.GlobalPosition.Y + 1.5f
        };
        bulletSpawnPoint.LookAt(markerAimTarget, Vector3.Up);

        var localLinearVelocity = GlobalBasis.Inverse() * state.LinearVelocity;
        localLinearVelocity.X = 0;

        var delta = (float)state.Step;

        switch (currentState)
        {
            case BossState.Shooting:
                dashReadinessTimer -= delta;
                UpdateCooldownBar();
                if (dashReadinessTimer <= 0)
                {
                    currentState = BossState.DashWindUp;
                    dashWindUpTimer = dashWindUpDuration;
                }
                else
                {
                    // Follow player at normal speed
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
                }
                break;

            case BossState.DashWindUp:
                dashWindUpTimer -= delta;
                UpdateCooldownBar();
                if (dashWindUpTimer <= 0)
                {
                    currentState = BossState.Charging;
                    chargeTimer = chargeDuration;
                }
                localLinearVelocity.Z = 0;
                animPlayer.Play(idleAnimation, customBlend: animationBlendAmount);
                break;

            case BossState.Charging:
                chargeTimer -= delta;
                UpdateCooldownBar();
                if (chargeTimer <= 0)
                {
                    currentState = BossState.DashCooldown;
                    dashCooldownTimer = dashCooldownAfterCharge;
                }
                else
                {
                    // Charge at player (full speed toward player)
                    localLinearVelocity.Z = -chargeSpeed;
                    animPlayer.Play(runAnimation, customBlend: animationBlendAmount);
                }
                break;

            case BossState.DashCooldown:
                dashCooldownTimer -= delta;
                UpdateCooldownBar();
                if (dashCooldownTimer <= 0)
                {
                    currentState = BossState.Shooting;
                    dashReadinessTimer = timeBeforeDashAvailable;
                }
                // Stand still during cooldown
                localLinearVelocity.Z = 0;
                animPlayer.Play(idleAnimation, customBlend: animationBlendAmount);
                break;
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

    readonly List<Bullet> spawnedBullets = new();

    [Export]
    AudioStreamPlayer? shootfx;

    public override void _Process(double delta)
    {
        // Only shoot when in Shooting state
        if (currentState != BossState.Shooting || !readyToFire || playerToFollow == null)
            return;

        {
            var newBullet = bulletScene.Instantiate<Bullet>();
            newBullet
                .SetDamageAmount(BulletDamage)
                .SetDamageAppliesTo(DamageManager.FarmerForceName);
            spawnedBullets.Add(newBullet);

            GetTree().CurrentScene.AddChild(newBullet);

            var aimTarget = playerToFollow.GlobalPosition with
            {
                Y = playerToFollow.GlobalPosition.Y + 1.5f
            };
            newBullet.GlobalPosition = bulletSpawnPoint.GlobalPosition;
            newBullet.LookAt(aimTarget, Vector3.Up);
            newBullet.RotateObjectLocal(Vector3.Up, (float)(-Math.PI / 2));
            newBullet.LinearVelocity = -bulletSpawnPoint.GlobalBasis.Z * bulletSpeed;
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
