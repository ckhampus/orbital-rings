using Godot;
using OrbitalRings.Data;

namespace OrbitalRings.Build;

/// <summary>
/// Static color palette for room block categories and visual metadata.
/// Warm pastel palette matching the cozy space station aesthetic.
/// Each category has a distinct color for instant recognition on the ring.
/// </summary>
public static class RoomColors
{
    // Category base colors — warm pastels
    public static readonly Color Housing = new(0.95f, 0.72f, 0.65f);
    public static readonly Color LifeSupport = new(0.65f, 0.88f, 0.75f);
    public static readonly Color Work = new(0.92f, 0.82f, 0.55f);
    public static readonly Color Comfort = new(0.80f, 0.70f, 0.90f);
    public static readonly Color Utility = new(0.70f, 0.78f, 0.85f);

    /// <summary>Flash color for invalid placement attempts.</summary>
    public static readonly Color InvalidFlash = new(0.95f, 0.25f, 0.25f, 0.7f);

    /// <summary>
    /// Returns the opaque base color for a room category.
    /// </summary>
    public static Color GetCategoryColor(RoomDefinition.RoomCategory category) => category switch
    {
        RoomDefinition.RoomCategory.Housing => Housing,
        RoomDefinition.RoomCategory.LifeSupport => LifeSupport,
        RoomDefinition.RoomCategory.Work => Work,
        RoomDefinition.RoomCategory.Comfort => Comfort,
        RoomDefinition.RoomCategory.Utility => Utility,
        _ => Housing
    };

    /// <summary>
    /// Returns a semi-transparent ghost color for placement preview (50% alpha).
    /// </summary>
    public static Color GetGhostColor(RoomDefinition.RoomCategory category)
    {
        Color baseColor = GetCategoryColor(category);
        return new Color(baseColor.R, baseColor.G, baseColor.B, 0.5f);
    }

    /// <summary>
    /// Returns the block height (in Godot units) for a room type.
    /// Each room has a slightly different height for visual variety within categories.
    /// </summary>
    public static float GetBlockHeight(string roomId) => roomId switch
    {
        "bunk_pod" => 0.25f,
        "sky_loft" => 0.35f,
        "air_recycler" => 0.22f,
        "garden_nook" => 0.30f,
        "workshop" => 0.28f,
        "craft_lab" => 0.32f,
        "star_lounge" => 0.24f,
        "reading_nook" => 0.28f,
        "storage_bay" => 0.20f,
        "comm_relay" => 0.38f,
        _ => 0.25f
    };
}
