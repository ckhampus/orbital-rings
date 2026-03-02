using Godot;

namespace OrbitalRings.Data;

/// <summary>
/// Template for a wish that citizens can express. Includes text variants
/// for natural variety and an explicit mapping to fulfilling room types.
/// </summary>
[GlobalClass]
public partial class WishTemplate : Resource
{
  /// <summary>
  /// Wish motivation categories per REQUIREMENTS WISH-04.
  /// </summary>
  public enum WishCategory
  {
    Social,
    Comfort,
    Curiosity,
    Variety
  }

  [ExportGroup("Wish Definition")]
  [Export] public string WishId { get; set; } = "";
  [Export] public WishCategory Category { get; set; }
  [Export] public string[] TextVariants { get; set; } = System.Array.Empty<string>();

  [ExportGroup("Fulfillment")]
  [Export] public string[] FulfillingRoomIds { get; set; } = System.Array.Empty<string>();
}
