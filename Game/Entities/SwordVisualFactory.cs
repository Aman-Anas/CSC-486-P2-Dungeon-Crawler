using Godot;

namespace Game.Entities;

static class SwordVisualFactory
{
    public static Node3D CreateVisual(bool firstPerson = false)
    {
        var root = new Node3D { Name = "SwordVisual" };

        var bladeMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.84f, 0.88f, 0.93f),
            Metallic = 0.95f,
            Roughness = 0.2f,
        };

        var guardMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.83f, 0.69f, 0.28f),
            Metallic = 0.8f,
            Roughness = 0.28f,
        };

        var gripMaterial = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.24f, 0.13f, 0.07f),
            Roughness = 0.88f,
        };

        AddMesh(
            root,
            "Blade",
            new BoxMesh { Size = new Vector3(0.08f, 1.8f, 0.05f) },
            bladeMaterial,
            new Vector3(0.0f, 1.15f, 0.0f),
            firstPerson
        );

        AddMesh(
            root,
            "Tip",
            new CylinderMesh
            {
                BottomRadius = 0.045f,
                TopRadius = 0.0f,
                Height = 0.22f,
                RadialSegments = 6,
                Rings = 1,
            },
            bladeMaterial,
            new Vector3(0.0f, 2.16f, 0.0f),
            firstPerson
        );

        AddMesh(
            root,
            "Guard",
            new BoxMesh { Size = new Vector3(0.45f, 0.08f, 0.12f) },
            guardMaterial,
            new Vector3(0.0f, 0.2f, 0.0f),
            firstPerson
        );

        AddMesh(
            root,
            "Grip",
            new CylinderMesh
            {
                TopRadius = 0.045f,
                BottomRadius = 0.045f,
                Height = 0.45f,
                RadialSegments = 8,
                Rings = 1,
            },
            gripMaterial,
            new Vector3(0.0f, -0.12f, 0.0f),
            firstPerson
        );

        AddMesh(
            root,
            "Pommel",
            new SphereMesh
            {
                Radius = 0.08f,
                Height = 0.16f,
                RadialSegments = 8,
                Rings = 8,
            },
            guardMaterial,
            new Vector3(0.0f, -0.38f, 0.0f),
            firstPerson
        );

        return root;
    }

    static void AddMesh(
        Node3D root,
        string name,
        Mesh mesh,
        Material material,
        Vector3 position,
        bool firstPerson
    )
    {
        var instance = new MeshInstance3D
        {
            Name = name,
            Mesh = mesh,
            MaterialOverride = material,
            Position = position,
            CastShadow = firstPerson
                ? GeometryInstance3D.ShadowCastingSetting.Off
                : GeometryInstance3D.ShadowCastingSetting.On,
        };

        root.AddChild(instance);
    }
}
