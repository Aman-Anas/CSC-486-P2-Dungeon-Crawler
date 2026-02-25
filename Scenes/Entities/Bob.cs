using Godot;
using System;

public partial class Bob : RigidBody3D
{
	// expose the path to the inspector via a property rather than a public
	// field and initialise it to the default (empty) path
	[Export]
	public NodePath TargetPath { get; set; } = new NodePath();

	[Export]
	public float speed = 4.0f;

	[Export]
	public float stopDistance = 2.0f;

	[Export]
	public float rotationSpeed = 5.0f; // radians per second

	Node3D? target;

	public override void _Ready()
	{
		// Use Character mode so setting LinearVelocity works without teleporting
		//tMode = RigidBody3D.Mode.Character;

		if (TargetPath != new NodePath())
		{
			target = GetNode<Node3D>(TargetPath);
		}
		else
		{
			var nodes = GetTree().GetNodesInGroup("player");
			if (nodes.Count > 0)
				target = nodes[0] as Node3D;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (target == null) return;

		var myPos = GlobalTransform.Origin;
		var targetPos = target.GlobalTransform.Origin;
		var toTarget = targetPos - myPos;

		var horizontal = new Vector3(toTarget.X, 0, toTarget.Z);
		var distance = horizontal.Length();

		if (distance <= stopDistance)
		{
			LinearVelocity = new Vector3(0, LinearVelocity.Y, 0);
			return;
		}

		// Compute shortest angle to face target (Y axis)
		var targetAngle = MathF.Atan2(horizontal.X, horizontal.Z);
		var currentAngle = Rotation.Y;
		var angleDiff = targetAngle - currentAngle;
		while (angleDiff > MathF.PI) angleDiff -= 2 * MathF.PI;
		while (angleDiff < -MathF.PI) angleDiff += 2 * MathF.PI;


		// Compute angular velocity (rad/s) to rotate toward the target without teleporting
		if (MathF.Abs(angleDiff) < 0.0001f)
		{
			AngularVelocity = Vector3.Zero;
		}
		else
		{
			var desired = angleDiff / (float)delta; // rad/s required to finish this frame
			var angularVelY = Mathf.Clamp(desired, -rotationSpeed, rotationSpeed);
			AngularVelocity = new Vector3(0, angularVelY, 0);
		}

		// Move forward along local -Z (Godot forward)
		var forward = -GlobalTransform.Basis.Z;
		LinearVelocity = new Vector3(forward.X * speed, LinearVelocity.Y, forward.Z * speed);
	}
}
