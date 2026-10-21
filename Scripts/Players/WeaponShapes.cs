using Godot;

/// <summary>Reads hit sizes from the collision shapes already set up in the weapon scenes.</summary>
public static class WeaponShapes
{
    /// <summary>
    /// World-space radius of the first sphere collision shape under the node,
    /// including every parent's scale. Returns fallback if there is none.
    /// </summary>
    public static float WorldRadius(Node3D owner, float fallback)
    {
        foreach (var child in owner.GetChildren())
        {
            if (child is CollisionShape3D shape && shape.Shape is SphereShape3D sphere)
                return sphere.Radius * shape.GlobalTransform.Basis.Scale.X;
        }
        return fallback;
    }
}
