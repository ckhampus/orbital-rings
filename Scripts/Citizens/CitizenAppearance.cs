using Godot;
using OrbitalRings.Data;

namespace OrbitalRings.Citizens;

/// <summary>
/// Static helper that creates capsule mesh visuals for citizens based on their CitizenData.
/// Each citizen gets a Node3D container with two MeshInstance3D children:
/// - Primary body capsule with body-type proportions and primary color
/// - Secondary color band (shorter, slightly wider capsule at midsection)
///
/// CRITICAL: Every material is a NEW StandardMaterial3D instance per citizen.
/// Never share material references -- prevents shared-material contamination
/// (Phase 2 lesson: individual materials per entity).
/// </summary>
public static class CitizenAppearance
{
    /// <summary>
    /// Creates a Node3D container with two capsule meshes representing a citizen.
    /// The primary capsule uses body-type proportions; the secondary band overlays
    /// at the midsection with a contrasting color.
    /// </summary>
    /// <param name="data">Citizen data with body type and color information.</param>
    /// <returns>Node3D with two MeshInstance3D children (primary body + color band).</returns>
    public static Node3D CreateCitizenMesh(CitizenData data)
    {
        var container = new Node3D();
        container.Name = "MeshContainer";

        // Determine body-type proportions
        float height = GetCapsuleHeight(data.Body);
        float radius = GetCapsuleRadius(data.Body);

        // Primary body capsule
        var primaryCapsule = new CapsuleMesh
        {
            Height = height,
            Radius = radius,
            RadialSegments = 16,
            Rings = 4
        };

        var primaryMaterial = new StandardMaterial3D
        {
            AlbedoColor = data.PrimaryColor
        };

        var primaryMesh = new MeshInstance3D
        {
            Name = "PrimaryMesh",
            Mesh = primaryCapsule,
            MaterialOverride = primaryMaterial
        };

        container.AddChild(primaryMesh);

        // Secondary color band (shorter, slightly wider capsule at midsection)
        var bandCapsule = new CapsuleMesh
        {
            Height = height * 0.35f,
            Radius = radius * 1.15f,
            RadialSegments = 16,
            Rings = 2
        };

        var bandMaterial = new StandardMaterial3D
        {
            AlbedoColor = data.SecondaryColor
        };

        var bandMesh = new MeshInstance3D
        {
            Name = "BandMesh",
            Mesh = bandCapsule,
            MaterialOverride = bandMaterial,
            Position = Vector3.Zero // Centered on parent
        };

        container.AddChild(bandMesh);

        return container;
    }

    /// <summary>
    /// Returns the capsule height for a given body type.
    /// Needed externally for Y positioning (standing on walkway surface).
    /// </summary>
    public static float GetCapsuleHeight(CitizenData.BodyType body)
    {
        return body switch
        {
            CitizenData.BodyType.Tall => 0.35f,
            CitizenData.BodyType.Short => 0.18f,
            CitizenData.BodyType.Round => 0.22f,
            _ => 0.22f
        };
    }

    /// <summary>
    /// Returns the capsule radius for a given body type.
    /// </summary>
    private static float GetCapsuleRadius(CitizenData.BodyType body)
    {
        return body switch
        {
            CitizenData.BodyType.Tall => 0.06f,
            CitizenData.BodyType.Short => 0.07f,
            CitizenData.BodyType.Round => 0.09f,
            _ => 0.07f
        };
    }
}
