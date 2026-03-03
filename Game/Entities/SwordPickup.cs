using Godot;

namespace Game.Entities;

public partial class SwordPickup : Area3D
{
    const float RotationSpeed = 1.4f;
    const float BobHeight = 0.08f;
    const float BobSpeed = 2.4f;
    const float DefaultPickupDelaySeconds = 0.15f;

    Node3D visualRoot = null!;
    float visualBaseHeight;
    float elapsed;
    bool pickedUp;
    ulong pickupEnabledAt;

    public SwordPickup()
    {
        Name = "SwordPickup";
        Monitoring = true;
        Monitorable = true;
        CollisionLayer = 0u;
        CollisionMask = 7u;
        SetPickupDelay(DefaultPickupDelaySeconds);
    }

    public override void _Ready()
    {
        var collisionShape = new CollisionShape3D
        {
            Name = "CollisionShape3D",
            Position = new Vector3(0.0f, 0.45f, 0.0f),
            Shape = new SphereShape3D { Radius = 0.85f },
        };
        AddChild(collisionShape);

        visualRoot = SwordVisualFactory.CreateVisual();
        visualRoot.Name = "VisualRoot";
        visualRoot.Position = new Vector3(0.0f, 0.14f, 0.0f);
        visualRoot.RotationDegrees = new Vector3(90.0f, 0.0f, 35.0f);
        AddChild(visualRoot);

        visualBaseHeight = visualRoot.Position.Y;
        BodyEntered += OnBodyEntered;
    }

    public override void _ExitTree()
    {
        BodyEntered -= OnBodyEntered;
    }

    public override void _Process(double delta)
    {
        if (pickedUp)
        {
            return;
        }

        elapsed += (float)delta;
        RotateY(RotationSpeed * (float)delta);

        var visualPosition = visualRoot.Position;
        visualPosition.Y = visualBaseHeight + (Mathf.Sin(elapsed * BobSpeed) * BobHeight);
        visualRoot.Position = visualPosition;

        TryAutoPickupOverlappingFarmer();
    }

    public void SetPickupDelay(double pickupDelaySeconds)
    {
        pickupEnabledAt = Time.GetTicksMsec() + (ulong)(pickupDelaySeconds * 1000.0);
    }

    void OnBodyEntered(Node3D body)
    {
        if (!CanBePickedUpBy(body, out var farmer))
        {
            return;
        }

        pickedUp = true;
        Monitoring = false;
        farmer!.EquipSword(GlobalPosition);
        QueueFree();
    }

    void TryAutoPickupOverlappingFarmer()
    {
        if (Time.GetTicksMsec() < pickupEnabledAt)
        {
            return;
        }

        foreach (var body in GetOverlappingBodies())
        {
            if (CanBePickedUpBy(body, out var farmer))
            {
                pickedUp = true;
                Monitoring = false;
                farmer!.EquipSword(GlobalPosition);
                QueueFree();
                return;
            }
        }
    }

    bool CanBePickedUpBy(Node body, out Farmer? farmer)
    {
        farmer = body as Farmer;
        return !pickedUp
            && Time.GetTicksMsec() >= pickupEnabledAt
            && farmer != null
            && !farmer.HasSword;
    }
}
