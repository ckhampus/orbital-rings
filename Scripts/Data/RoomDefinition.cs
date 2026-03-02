using Godot;

namespace OrbitalRings.Data;

/// <summary>
/// Defines a room type that can be placed on the orbital ring.
/// Purely mechanical: category, size constraints, and stats.
/// Visual flavor comes from art and room names, not data tags.
/// </summary>
[GlobalClass]
public partial class RoomDefinition : Resource
{
  /// <summary>
  /// The five functional categories for rooms on the station.
  /// </summary>
  public enum RoomCategory
  {
    Housing,
    LifeSupport,
    Work,
    Comfort,
    Utility
  }

  [ExportGroup("Identity")]
  [Export] public string RoomName { get; set; } = "";
  [Export] public string RoomId { get; set; } = "";

  [ExportGroup("Placement")]
  [Export] public RoomCategory Category { get; set; }
  [Export(PropertyHint.Range, "1,3")] public int MinSegments { get; set; } = 1;
  [Export(PropertyHint.Range, "1,3")] public int MaxSegments { get; set; } = 1;

  [ExportGroup("Stats")]
  [Export] public int BaseCapacity { get; set; }
  [Export] public float Effectiveness { get; set; } = 1.0f;
}
