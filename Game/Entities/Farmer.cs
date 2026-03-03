using System;
using System.Linq;
using Game;
using Game.Entities;
using Game.UI;
using Godot;
using GodotTask;

namespace Game.Entities;

[GlobalClass]
public partial class Farmer : RigidBody3D
{
    public bool MovementEnabled { get; set; } = true;

    /// <summary>
    /// Location for enemies to target the player
    /// </summary>
    [Export]
    public Node3D TargetPosition { get; set; } = null!;

    /// <summary>
    /// Raycast to check if we're on solid ground
    /// </summary>
    [Export]
    Node3D floorSensorParent = null!;

    [Export]
    AnimationPlayer? player = null;

    ////////////////////////////////////////

    /// Constants go here
    [Export]
    float MOVEMENT_FORCE = 30;

    [Export]
    float AIR_MOVEMENT_FORCE = 30;

    [Export]
    float RUN_SPEED = 10;

    [Export]
    float WALK_SPEED = 6;

    // [Export]
    // float JUMP_MOVE_SPEED = 8;

    [Export]
    float GRAPPLE_FORCE = 50;

    [Export]
    float maxMovementSpeed = 6;

    bool running;

    Vector3 movementVec = new(0, 0, 0);

    // Jumping

    [Export]
    Vector3 JUMP_IMPULSE = new(0, 5.5f, 0);

    [Export]
    ulong MAX_JUMP_RESET_TIME = 4000; // ms

    ////////////////////////////////////////

    bool justJumped;
    ulong timeJumped;

    // Mouselook
    [Export]
    Node3D yawTarget = null!;

    [Export]
    Node3D pitchTarget = null!;

    [Export]
    Node3D headPosition = null!;

    readonly float MIN_PITCH = Mathf.DegToRad(-90.0f);
    readonly float MAX_PITCH = Mathf.DegToRad(90.0f);

    const float GRAVITY_CORRECTION_SPEED = 4.0f;
    const float ROTATION_SPEED = 7f;

    [Export]
    RayCast3D grappleCast = null!;
    Vector3 currentGrapplePos;
    Node3D? currentGrappleNode = null;

    [Export]
    PackedScene returnToTitleScene = null!;

    [Export]
    public bool SpawnStartingSword { get; set; } = true;

    [Export]
    public Vector2 StartingSwordSpawnOffset { get; set; } = new(1.4f, 2.0f);

    [Export]
    public float StartingSwordSpawnHeight { get; set; } = 2.5f;

    [Export]
    public float StartingSwordRayDistance { get; set; } = 6.0f;

    [Export]
    public float StartingSwordFloorClearance { get; set; } = 0.15f;

    [Export]
    public Vector2 WeaponSwapDropOffset { get; set; } = new(1.2f, 1.5f);

    [Export]
    public float WeaponPickupDelay { get; set; } = 0.35f;

    [Export]
    public Vector3 EquippedSwordPosition { get; set; } = new(0.32f, -0.28f, -0.55f);

    [Export]
    public Vector3 EquippedSwordRotationDegrees { get; set; } = new(-70.0f, 12.0f, 100.0f);

    [Export]
    public float SwordSwingDuration { get; set; } = 0.11f;

    [Export]
    public float SwordSwingRecoverDuration { get; set; } = 0.17f;

    [Export]
    public float SwordSwingCooldown { get; set; } = 0.35f;

    [Export]
    public Vector3 SwordSwingPositionOffset { get; set; } = new(0.22f, -0.14f, 0.14f);

    [Export]
    public Vector3 SwordSwingRotationOffsetDegrees { get; set; } = new(-32.0f, -72.0f, -28.0f);

    [Export]
    public float SwordHitRange { get; set; } = 1.75f;

    [Export]
    public float SwordHitRadius { get; set; } = 1.0f;

    [Export]
    public int SwordHitMaxResults { get; set; } = 12;

    Camera3D? playerCamera;
    Gun? equippedGun;
    Transform3D equippedGunTransform = Transform3D.Identity;
    Node3D? swordHolder;
    Node3D? equippedSword;
    bool hasSword;
    bool startingSwordSpawned;
    bool swordSwingActive;
    bool swordSwingRecovering;
    float swordSwingTimer;
    ulong swordSwingAvailableAt;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Need this to capture the mouse of course
        Input.MouseMode = Input.MouseModeEnum.Captured;
        yawTarget.TopLevel = true;

        playerCamera = pitchTarget.GetNodeOrNull<Camera3D>("Camera3D");
        equippedGun = playerCamera?.GetNodeOrNull<Gun>("Pistol");
        if (equippedGun != null)
        {
            equippedGunTransform = equippedGun.Transform;
            equippedGun.SetEquippedState();
        }
        swordHolder = EnsureSwordHolder();

        progressBar.SetCoolValue(Manager.Instance.Data.CurrentHealth);
    }

    void UpdateHeadOrientation()
    {
        yawTarget.Orthonormalize();
        yawTarget.GlobalPosition = headPosition.GlobalPosition;
        var yawUpDiff = new Quaternion(yawTarget.GlobalBasis.Y, GlobalBasis.Y).Normalized();
        var axis = yawUpDiff.GetAxis();

        // Check to ensure the quaternion is valid and not all zeros
        if (yawUpDiff.LengthSquared() == 0 || axis.LengthSquared() == 0)
            return;

        yawTarget.Rotate(axis.Normalized(), yawUpDiff.GetAngle());

        // mouseLookRotationTarget.GlobalRotation = yawTarget.GlobalRotation;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Direct mouselook for head itself
        if (@event is InputEventMouseMotion motion)
        {
            var sensitivity = Manager.Instance.Config.MouseSensitivity;

            yawTarget.RotateObjectLocal(Vector3.Up, -motion.Relative.X * sensitivity);

            var pitchRot = pitchTarget.Rotation;
            pitchRot.X = Mathf.Clamp(
                pitchRot.X + (-motion.Relative.Y * sensitivity),
                MIN_PITCH,
                MAX_PITCH
            );
            pitchTarget.Rotation = pitchRot;
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        Orthonormalize();

        var inputVec = Input.GetVector(
            GameActions.PlayerStrafeLeft,
            GameActions.PlayerStrafeRight,
            GameActions.PlayerForward,
            GameActions.PlayerBackward
        );

        var touchingFloor = false;

        foreach (var floorSensor in floorSensorParent.GetChildren().Cast<RayCast3D>())
        {
            // Detect whether we're touching the floor (with feet)
            if (floorSensor.IsColliding())
            {
                touchingFloor = true;
                break;
            }
        }

        // Movement
        movementVec.X = inputVec.X;
        movementVec.Y = 0;
        movementVec.Z = inputVec.Y;

        // Convert our global linear velocity to local and remove Y
        var actualLocalVelocity = GlobalBasis.Inverse() * state.LinearVelocity;
        Vector3 localVeloCopy = actualLocalVelocity;
        actualLocalVelocity.Y = 0;

        running =
            Input.IsActionPressed(GameActions.PlayerRun)
            || Input.IsMouseButtonPressed(MouseButton.Right);

        maxMovementSpeed = running ? RUN_SPEED : WALK_SPEED;

        var intendedLocalVelocity = maxMovementSpeed * movementVec;

        // Find the difference between them and use it to apply a force
        var diffVelo = intendedLocalVelocity - actualLocalVelocity;

        // Only control our own movement when touching the floor
        if (touchingFloor)
        {
            state.ApplyCentralForce(GlobalBasis * (diffVelo.LimitLength(1) * MOVEMENT_FORCE));
        }
        else
        {
            state.ApplyCentralForce(GlobalBasis * (diffVelo.LimitLength(1) * AIR_MOVEMENT_FORCE));
        }

        // Jumping

        // Reset the jump flag if we're in the air or a min time elapsed


        if (!touchingFloor)
        {
            justJumped = false;
        }
        else
        {
            if ((Time.GetTicksMsec() - timeJumped) > MAX_JUMP_RESET_TIME)
            {
                justJumped = false;
            }
        }

        // Dev mode jetpack
        // if (false && Input.IsActionPressed(GameActions.PlayerJump))
        // {
        //     state.ApplyCentralImpulse(GlobalBasis * Vector3.Up * 0.3f);
        // }

        if (Input.IsActionPressed(GameActions.PlayerJump) && touchingFloor && !justJumped)
        {
            state.ApplyCentralImpulse(
                GlobalBasis * (JUMP_IMPULSE + new Vector3(0, -localVeloCopy.Y * Mass, 0))
            );
            justJumped = true;
            timeJumped = Time.GetTicksMsec();
        }

        // Get the current gravity direction and our down direction (both global)
        var currentGravityDir = state.TotalGravity.Normalized();

        var currentDownDir = -GlobalBasis.Y;

        // Find the rotation difference between these two
        var rotationDifference = new Quaternion(currentDownDir, currentGravityDir);

        // Turn it into an euler and multiply by our gravity correction speed
        var gravityCorrectionVelo = rotationDifference.Normalized();

        // Before assigning gravity correction, add mouselook
        var newLocalAngVelo = gravityCorrectionVelo.GetEuler() * GRAVITY_CORRECTION_SPEED;

        // Get the rotation difference for our head
        var mouseLookDiff = new Quaternion(GlobalBasis.Z, yawTarget.GlobalBasis.Z)
            .Normalized()
            .GetEuler();

        // Put into local coordinates
        mouseLookDiff = GlobalBasis.Inverse() * mouseLookDiff;

        // Remove extraneous rotation (only want mouselook to affect Y)
        mouseLookDiff.X = 0;
        mouseLookDiff.Z = 0;

        // Add it to our new velocity (after making it global)
        newLocalAngVelo += GlobalBasis * (mouseLookDiff * ROTATION_SPEED);

        /**
        Get our final angular velocity. It would be more realistic to use torque,
        but velocity is a bit easier to work with. If needed, torque can be used though.
        */
        state.AngularVelocity = newLocalAngVelo;

        if (!IsInstanceValid(currentGrappleNode))
        {
            currentGrappleNode = null;
            currentGrapplePos = Vector3.Zero;
        }
        if (!Input.IsMouseButtonPressed(MouseButton.Right))
        {
            currentGrapplePos = Vector3.Zero;
            currentGrappleNode = null;
        }

        if (Input.IsMouseButtonPressed(MouseButton.Right) && currentGrappleNode == null)
        {
            if (grappleCast.IsColliding() && IsInstanceValid(grappleCast.GetCollider()))
            {
                currentGrappleNode = (Node3D)grappleCast.GetCollider();
                var hitPoint = grappleCast.GetCollisionPoint();
                currentGrapplePos = currentGrappleNode.ToLocal(hitPoint);
            }
        }

        if (currentGrappleNode != null)
        {
            var targetPoint = (currentGrappleNode.ToGlobal(currentGrapplePos));
            // GD.Print("point", actualPoint);
            var forceDir = ((targetPoint) - GlobalPosition).Normalized();
            state.ApplyCentralForce(GRAPPLE_FORCE * forceDir);
        }
    }

    [Export]
    BoneAttachment3D glowyEndPos = null!;

    [Export]
    Node3D glowyThing = null!;

    StringName runAnim = "BAKED_Run";
    StringName walkAnim = "BAKED_Run";
    StringName idleAnim = "BAKED_Run";

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        UpdateHeadOrientation();

        TryStartSwordSwing();
        UpdateSwordSwing(delta);

        var localVelo = GlobalBasis.Inverse() * LinearVelocity;
        localVelo.Y = 0;

        StringName currentAnimation;

        // Switch between idle and run anims
        if (movementVec.LengthSquared() > 0)
        {
            if (running && (localVelo.LengthSquared() > (WALK_SPEED * WALK_SPEED)))
            {
                currentAnimation = runAnim;
            }
            else
            {
                currentAnimation = walkAnim;
            }
        }
        else
        {
            currentAnimation = idleAnim;
        }

        player?.Play(currentAnimation, customBlend: 0.5);

        if (IsInstanceValid(currentGrappleNode) && (currentGrappleNode != null))
        {
            var targetPoint = (currentGrappleNode.ToGlobal(currentGrapplePos));
            glowyEndPos.GlobalPosition = targetPoint;
            glowyThing.Visible = true;
        }
        else
        {
            glowyThing.Visible = false;
        }
    }

    Node3D? EnsureSwordHolder()
    {
        if (swordHolder != null && IsInstanceValid(swordHolder))
        {
            return swordHolder;
        }

        if (playerCamera == null || !IsInstanceValid(playerCamera))
        {
            return null;
        }

        var existingHolder = playerCamera.GetNodeOrNull<Node3D>("SwordHolder");
        if (existingHolder != null)
        {
            swordHolder = existingHolder;
            return swordHolder;
        }

        swordHolder = new Node3D { Name = "SwordHolder" };
        playerCamera.AddChild(swordHolder);
        return swordHolder;
    }

    public bool HasSword => hasSword;
    public bool HasGun => equippedGun != null && IsInstanceValid(equippedGun) && equippedGun.IsEquipped;

    public void EquipSword(Vector3? swapPosition = null)
    {
        if (hasSword)
        {
            return;
        }

        if (HasGun)
        {
            DropGunAt(swapPosition ?? FindWeaponDropPosition());
        }

        hasSword = true;

        var holder = EnsureSwordHolder();
        if (holder == null)
        {
            return;
        }

        if (equippedSword != null && IsInstanceValid(equippedSword))
        {
            equippedSword.QueueFree();
        }

        equippedSword = SwordVisualFactory.CreateVisual(firstPerson: true);
        equippedSword.Name = "EquippedSword";
        holder.AddChild(equippedSword);
        equippedSword.Position = EquippedSwordPosition;
        equippedSword.RotationDegrees = EquippedSwordRotationDegrees;
        equippedSword.Scale = Vector3.One * 0.35f;
        ResetSwordSwing();
    }

    public void EquipGun(Gun gun)
    {
        if (playerCamera == null || !IsInstanceValid(playerCamera) || !IsInstanceValid(gun))
        {
            return;
        }

        var swapPosition = gun.GlobalPosition;

        if (hasSword)
        {
            DropSwordAt(swapPosition);
        }

        equippedGun = gun;
        equippedGun.Reparent(playerCamera);
        equippedGun.Transform = equippedGunTransform;
        equippedGun.SetEquippedState();
    }

    void DropGunAt(Vector3 worldPosition)
    {
        if (equippedGun == null || !IsInstanceValid(equippedGun))
        {
            return;
        }

        var currentScene = GetTree().CurrentScene;
        if (currentScene == null)
        {
            return;
        }

        equippedGun.Reparent(currentScene);
        equippedGun.GlobalPosition = worldPosition;
        equippedGun.RotationDegrees = new Vector3(0.0f, yawTarget.RotationDegrees.Y, 90.0f);
        equippedGun.SetDroppedState(WeaponPickupDelay);
    }

    void DropSwordAt(Vector3 worldPosition)
    {
        hasSword = false;
        ResetSwordSwing();

        if (equippedSword != null && IsInstanceValid(equippedSword))
        {
            equippedSword.QueueFree();
            equippedSword = null;
        }

        var currentScene = GetTree().CurrentScene;
        if (currentScene == null)
        {
            return;
        }

        var swordPickup = new SwordPickup();
        currentScene.AddChild(swordPickup);
        swordPickup.GlobalPosition = worldPosition;
        swordPickup.SetPickupDelay(WeaponPickupDelay);
    }

    void TryStartSwordSwing()
    {
        if (
            !hasSword
            || equippedSword == null
            || !IsInstanceValid(equippedSword)
            || swordSwingActive
            || Time.GetTicksMsec() < swordSwingAvailableAt
            || !Input.IsActionJustPressed(GameActions.PlayerFire)
        )
        {
            return;
        }

        swordSwingActive = true;
        swordSwingRecovering = false;
        swordSwingTimer = 0.0f;
        swordSwingAvailableAt = Time.GetTicksMsec() + (ulong)(SwordSwingCooldown * 1000.0);
        PerformSwordHit();
    }

    void UpdateSwordSwing(double delta)
    {
        if (equippedSword == null || !IsInstanceValid(equippedSword))
        {
            return;
        }

        if (!swordSwingActive)
        {
            ApplySwordPose(0.0f);
            return;
        }

        swordSwingTimer += (float)delta;

        if (!swordSwingRecovering)
        {
            var attackProgress = Mathf.Clamp(swordSwingTimer / SwordSwingDuration, 0.0f, 1.0f);
            ApplySwordPose(Mathf.SmoothStep(0.0f, 1.0f, attackProgress));

            if (attackProgress >= 1.0f)
            {
                swordSwingRecovering = true;
                swordSwingTimer = 0.0f;
            }

            return;
        }

        var recoverProgress = Mathf.Clamp(swordSwingTimer / SwordSwingRecoverDuration, 0.0f, 1.0f);
        ApplySwordPose(1.0f - Mathf.SmoothStep(0.0f, 1.0f, recoverProgress));

        if (recoverProgress >= 1.0f)
        {
            ResetSwordSwing();
        }
    }

    void ApplySwordPose(float swingBlend)
    {
        if (equippedSword == null || !IsInstanceValid(equippedSword))
        {
            return;
        }

        equippedSword.Position = EquippedSwordPosition.Lerp(
            EquippedSwordPosition + SwordSwingPositionOffset,
            swingBlend
        );
        equippedSword.RotationDegrees = EquippedSwordRotationDegrees.Lerp(
            EquippedSwordRotationDegrees + SwordSwingRotationOffsetDegrees,
            swingBlend
        );
    }

    void ResetSwordSwing()
    {
        swordSwingActive = false;
        swordSwingRecovering = false;
        swordSwingTimer = 0.0f;
        ApplySwordPose(0.0f);
    }

    void PerformSwordHit()
    {
        var swingOrigin = playerCamera?.GlobalPosition ?? headPosition.GlobalPosition;
        var swingForward = -(playerCamera?.GlobalBasis.Z ?? yawTarget.GlobalBasis.Z).Normalized();
        var hitCenter = swingOrigin + (swingForward * SwordHitRange);

        var sphere = new SphereShape3D { Radius = SwordHitRadius };
        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = sphere,
            Transform = new Transform3D(Basis.Identity, hitCenter),
            CollideWithBodies = true,
            CollideWithAreas = false,
        };
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        var results = GetWorld3D().DirectSpaceState.IntersectShape(query, SwordHitMaxResults);
        foreach (Godot.Collections.Dictionary result in results)
        {
            if (!result.TryGetValue("collider", out var colliderVariant))
            {
                continue;
            }

            if (TryKillSwordTarget(colliderVariant.AsGodotObject()))
            {
                continue;
            }
        }
    }

    bool TryKillSwordTarget(GodotObject? collider)
    {
        if (collider is not Node node || !IsInstanceValid(node))
        {
            return false;
        }

        if (node is NewBurger burger)
        {
            burger.Kill();
            return true;
        }

        if (node is NewBob bob)
        {
            bob.QueueFree();
            return true;
        }

        if (node.HasMethod("Kill"))
        {
            node.Call("Kill");
            return true;
        }

        if (node.HasMeta(EnemyMeta))
        {
            node.QueueFree();
            return true;
        }

        return false;
    }

    void TrySpawnStartingSword()
    {
        if (!SpawnStartingSword || startingSwordSpawned || hasSword)
        {
            return;
        }

        var currentScene = GetTree().CurrentScene;
        if (currentScene == null)
        {
            return;
        }

        var swordPickup = new SwordPickup();
        currentScene.AddChild(swordPickup);
        swordPickup.GlobalPosition = FindStartingSwordSpawnPosition();
        swordPickup.SetPickupDelay(0.15f);
        startingSwordSpawned = true;
    }

    Vector3 FindStartingSwordSpawnPosition()
    {
        return FindGroundPickupPosition(StartingSwordSpawnOffset);
    }

    Vector3 FindWeaponDropPosition()
    {
        return FindGroundPickupPosition(WeaponSwapDropOffset);
    }

    Vector3 FindGroundPickupPosition(Vector2 planarOffset)
    {
        var up = GlobalBasis.Y.Normalized();

        var forward = -yawTarget.GlobalBasis.Z;
        forward -= up * forward.Dot(up);
        if (forward.LengthSquared() <= Mathf.Epsilon)
        {
            forward = -GlobalBasis.Z;
            forward -= up * forward.Dot(up);
        }
        forward = forward.Normalized();

        var right = yawTarget.GlobalBasis.X;
        right -= up * right.Dot(up);
        if (right.LengthSquared() <= Mathf.Epsilon)
        {
            right = GlobalBasis.X;
            right -= up * right.Dot(up);
        }
        right = right.Normalized();

        var offset = (right * planarOffset.X) + (forward * planarOffset.Y);
        var rayStart = GlobalPosition + offset + (up * StartingSwordSpawnHeight);
        var rayEnd = rayStart - (up * StartingSwordRayDistance);

        var query = PhysicsRayQueryParameters3D.Create(rayStart, rayEnd);
        query.CollideWithBodies = true;
        query.CollideWithAreas = false;
        query.Exclude = new Godot.Collections.Array<Rid> { GetRid() };

        var result = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count > 0)
        {
            return result["position"].AsVector3() + (up * StartingSwordFloorClearance);
        }

        return GlobalPosition + offset;
    }

    // Whether or not we can take damage. Set to false during the invulnerability timer
    bool canTakeDamage = true;

    // Enemies should be marked with this string metadata
    public readonly StringName EnemyMeta = "enemy";

    // Time in seconds to stay invulnerable after a hit
    [Export]
    public float InvulnerabilityTime { get; set; } = 0.5f;

    [Export]
    FancyProgressBar progressBar = null!;

    [Export]
    AnimationPlayer damagePlayer = null!;
    StringName damageAnimName = "takedamage";

    public override void _PhysicsProcess(double delta)
    {
        TrySpawnStartingSword();

        // Figure out if we're touching an enemy
        var colliding = GetCollidingBodies();
        bool touchingEnemy = false;
        int amountToReduceHealth = 0;
        foreach (var collider in colliding)
        {
            if (collider.HasMeta(EnemyMeta))
            {
                touchingEnemy = true;

                // Grab the amount to reduce health by
                amountToReduceHealth = (int)collider.GetMeta(EnemyMeta);
                break;
            }
        }

        // Take damage
        if (canTakeDamage && touchingEnemy)
        {
            // Reduce our health
            Manager.Instance.Data.CurrentHealth -= amountToReduceHealth;
            progressBar.SetCoolValue(Manager.Instance.Data.CurrentHealth);
            damagePlayer.Play(damageAnimName);

            if (Manager.Instance.Data.CurrentHealth < 0)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                GetTree().ChangeSceneToPacked(returnToTitleScene);
            }

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
}
