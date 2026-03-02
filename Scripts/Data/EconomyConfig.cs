using Godot;

namespace OrbitalRings.Data;

/// <summary>
/// Centralized economy balance configuration. All credit costs, income rates,
/// and multipliers live here for easy iteration -- not spread across RoomDefinitions.
/// </summary>
[GlobalClass]
public partial class EconomyConfig : Resource
{
  [ExportGroup("Income")]
  [Export] public float PassiveIncomePerCitizen { get; set; } = 1.0f;
  [Export] public float WorkBonusMultiplier { get; set; } = 1.5f;
  [Export] public float HappinessMultiplierCap { get; set; } = 2.0f;

  [ExportGroup("Costs")]
  [Export] public int BaseRoomCost { get; set; } = 100;
  [Export] public float SizeDiscountFactor { get; set; } = 0.85f;
  [Export] public float DemolishRefundRatio { get; set; } = 0.5f;

  [ExportGroup("Starting Values")]
  [Export] public int StartingCredits { get; set; } = 500;
}
