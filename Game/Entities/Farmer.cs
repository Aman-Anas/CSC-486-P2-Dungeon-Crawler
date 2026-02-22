using System;
using System.Linq;
using Game;
using Game.Entities;
using Godot;

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
    float RUN_SPEED = 6;

    [Export]
    float WALK_SPEED = 4;

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
    ulong MIN_JUMP_RESET_TIME = 1000; // ms

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

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Need this to capture the mouse of course
        Input.MouseMode = Input.MouseModeEnum.Captured;

        // Add a callback to update the current anim state
        // player.CurrentAnimationChanged += (_) => Data.AnimHelper.NetUpdate();
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
            }
        }

        // Movement
        movementVec.X = inputVec.X;
        movementVec.Y = 0;
        movementVec.Z = inputVec.Y;

        // Convert our global linear velocity to local and remove Y
        var actualLocalVelocity = GlobalBasis.Inverse() * state.LinearVelocity;
        actualLocalVelocity.Y = 0;

        running =
            Input.IsActionPressed(GameActions.PlayerRun)
            || Input.IsMouseButtonPressed(MouseButton.Right);

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
        if ((!touchingFloor) || ((Time.GetTicksMsec() - timeJumped) > MIN_JUMP_RESET_TIME))
        {
            justJumped = false;
        }

        // Dev mode jetpack
        if (false && Input.IsActionPressed(GameActions.PlayerJump))
        {
            state.ApplyCentralImpulse(GlobalBasis * Vector3.Up * 0.3f);
        }

        if (Input.IsActionPressed(GameActions.PlayerJump) && touchingFloor && !justJumped)
        {
            state.ApplyCentralImpulse(GlobalBasis * JUMP_IMPULSE);
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

    public override void _PhysicsProcess(double delta)
    {
        if (!grappleCast.IsColliding())
        {
            return;
        }

        // CSGBoxes are detected, but aren't actually CollisionObject3Ds
        if (grappleCast.GetCollider() is not CollisionObject3D collisionObj)
        {
            return;
        }
    }
}
