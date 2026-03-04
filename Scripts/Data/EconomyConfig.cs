using Godot;

namespace OrbitalRings.Data;

/// <summary>
/// Centralized economy balance configuration. All credit costs, income rates,
/// and multipliers live here for easy iteration -- not spread across RoomDefinitions.
///
/// Default values are calibrated from the economy balance spreadsheet
/// (.planning/phases/03-economy-foundation/economy-balance.md).
/// </summary>
[GlobalClass]
public partial class EconomyConfig : Resource
{
    [ExportGroup("Income")]

    /// <summary>Trickle income at 0 citizens so the player is never stuck.</summary>
    [Export] public float BaseStationIncome { get; set; } = 1.0f;

    /// <summary>Coefficient applied to sqrt(citizenCount) for per-citizen income.</summary>
    [Export] public float PassiveIncomePerCitizen { get; set; } = 2.0f;

    /// <summary>Credits per worker per tick. Kept modest to avoid runaway.</summary>
    [Export] public float WorkBonusMultiplier { get; set; } = 1.25f;

    /// <summary>
    /// Maximum happiness multiplier on income. Formula: 1.0 + (happiness * (cap - 1.0)).
    /// Capped at 1.3 to keep feedback loop bounded (30% bonus at perfect happiness).
    /// </summary>
    [Export] public float HappinessMultiplierCap { get; set; } = 1.3f;

    /// <summary>Seconds between income ticks.</summary>
    [Export] public float IncomeTickInterval { get; set; } = 5.5f;

    [ExportGroup("Tier Income Multipliers")]

    /// <summary>
    /// Income multiplier at Quiet tier. Default 1.0 (no bonus).
    /// Applies to all income: base station + citizen passive + work bonus.
    /// </summary>
    [Export] public float IncomeMultQuiet   { get; set; } = 1.0f;

    /// <summary>Income multiplier at Cozy tier. Default 1.1 (10% bonus).</summary>
    [Export] public float IncomeMultCozy    { get; set; } = 1.1f;

    /// <summary>Income multiplier at Lively tier. Default 1.2 (20% bonus).</summary>
    [Export] public float IncomeMultLively  { get; set; } = 1.2f;

    /// <summary>Income multiplier at Vibrant tier. Default 1.3 (30% bonus).</summary>
    [Export] public float IncomeMultVibrant { get; set; } = 1.3f;

    /// <summary>Income multiplier at Radiant tier. Default 1.4 (40% bonus).</summary>
    [Export] public float IncomeMultRadiant { get; set; } = 1.4f;

    [ExportGroup("Costs")]

    /// <summary>Base cost anchor for the room cost formula.</summary>
    [Export] public int BaseRoomCost { get; set; } = 100;

    /// <summary>
    /// Per-segment discount exponent. Applied as sizeDiscount^(segments-1).
    /// 0.92 gives ~8% discount per additional segment.
    /// </summary>
    [Export] public float SizeDiscountFactor { get; set; } = 0.92f;

    /// <summary>Fraction of build cost refunded on demolish (50%).</summary>
    [Export] public float DemolishRefundRatio { get; set; } = 0.5f;

    /// <summary>Cost multiplier for outer-row segments (physically larger).</summary>
    [Export] public float OuterRowCostMultiplier { get; set; } = 1.1f;

    [ExportGroup("Category Cost Multipliers")]

    /// <summary>Housing is cheapest -- essential for onboarding.</summary>
    [Export] public float HousingCostMultiplier { get; set; } = 0.7f;

    /// <summary>Life support slightly above Housing, still affordable.</summary>
    [Export] public float LifeSupportCostMultiplier { get; set; } = 0.85f;

    /// <summary>Work rooms are baseline cost.</summary>
    [Export] public float WorkCostMultiplier { get; set; } = 1.0f;

    /// <summary>Comfort rooms carry a premium.</summary>
    [Export] public float ComfortCostMultiplier { get; set; } = 1.15f;

    /// <summary>Utility rooms are the most expensive category.</summary>
    [Export] public float UtilityCostMultiplier { get; set; } = 1.3f;

    [ExportGroup("Starting Values")]

    /// <summary>
    /// Credits the player begins with. 750 affords 5-6 cheap 1-segment
    /// Housing/LifeSupport rooms with budget for one medium room.
    /// </summary>
    [Export] public int StartingCredits { get; set; } = 750;
}
