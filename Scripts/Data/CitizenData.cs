using Godot;

namespace OrbitalRings.Data;

/// <summary>
/// Citizen identity and appearance data. Strictly v1: name + visual traits only.
/// No personality traits, relationships, or schedules -- those are v2 territory.
/// </summary>
[GlobalClass]
public partial class CitizenData : Resource
{
  /// <summary>
  /// Visual body type variations for citizen models.
  /// </summary>
  public enum BodyType
  {
    Tall,
    Short,
    Round
  }

  [ExportGroup("Identity")]
  [Export] public string CitizenName { get; set; } = "";

  [ExportGroup("Appearance")]
  [Export] public BodyType Body { get; set; }
  [Export] public Color PrimaryColor { get; set; } = Colors.White;
  [Export] public Color SecondaryColor { get; set; } = Colors.Gray;
  [Export] public string Accessory { get; set; } = "";
}
