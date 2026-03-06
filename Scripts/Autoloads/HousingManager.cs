using Godot;
using OrbitalRings.Data;

namespace OrbitalRings.Autoloads;

/// <summary>
/// Manages citizen-to-home-room assignment. Owns the mapping of citizens to
/// housing rooms and emits CitizenAssignedHome/CitizenUnhoused events.
///
/// Registered as an Autoload in project.godot (8th singleton, before SaveManager).
/// Access via HousingManager.Instance.
///
/// Phase 14: skeleton only (type + singleton pattern).
/// Phase 15: full assignment logic.
/// Phase 16: capacity transfer from HappinessManager.
/// </summary>
public partial class HousingManager : Node
{
    /// <summary>Singleton instance, set in _EnterTree().</summary>
    public static HousingManager Instance { get; private set; }

    /// <summary>HousingConfig resource -- set via Inspector or loaded from default path.</summary>
    [Export] public HousingConfig Config { get; set; }

    public override void _EnterTree()
    {
        Instance = this;
    }
}
