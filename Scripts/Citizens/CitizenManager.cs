using System.Collections.Generic;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Core;
using OrbitalRings.Data;
using OrbitalRings.Ring;

namespace OrbitalRings.Citizens;

/// <summary>
/// Autoload singleton managing citizen lifecycle: spawning, tracking, and removal.
/// Spawns 5 starter citizens at game start with distinct appearances, evenly spaced
/// around the walkway ring. Updates EconomyManager with citizen count for income calculation.
///
/// Handles click detection via ray-plane intersection and polar proximity,
/// showing CitizenInfoPanel with emission glow on selected citizen.
///
/// Extends SafeNode for consistent signal lifecycle management.
/// Access via CitizenManager.Instance (set in _Ready, before scene nodes enter tree).
/// </summary>
public partial class CitizenManager : SafeNode
{
    /// <summary>
    /// Singleton instance. Set in _Ready(). Guaranteed non-null after Autoloads initialize
    /// because Autoloads initialize before any scene nodes enter the tree.
    /// </summary>
    public static CitizenManager Instance { get; private set; }

    /// <summary>All active citizen nodes managed by this system.</summary>
    private readonly List<CitizenNode> _citizens = new();

    /// <summary>Current number of active citizens.</summary>
    public int CitizenCount => _citizens.Count;

    /// <summary>Read-only access to citizen list (for click detection iteration).</summary>
    public IReadOnlyList<CitizenNode> Citizens => _citizens;

    // -------------------------------------------------------------------------
    // Curated color palette (cozy/warm aesthetic)
    // -------------------------------------------------------------------------

    private static readonly Color[] Palette =
    {
        new Color(0.95f, 0.6f, 0.5f),   // salmon
        new Color(0.6f, 0.85f, 0.7f),   // mint
        new Color(0.55f, 0.65f, 0.95f), // periwinkle
        new Color(0.95f, 0.85f, 0.5f),  // buttercup
        new Color(0.85f, 0.55f, 0.85f), // lavender
        new Color(0.5f, 0.85f, 0.85f),  // teal
        new Color(0.95f, 0.7f, 0.4f),   // peach
        new Color(0.7f, 0.9f, 0.55f),   // lime
    };

    // -------------------------------------------------------------------------
    // Ring reference (for SegmentGrid access)
    // -------------------------------------------------------------------------

    private SegmentGrid _grid;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        base._Ready();
        Instance = this;

        // Find the RingVisual and cache its grid for citizen visit occupancy checks
        // Same pattern as BuildManager: FindChild from Root
        var ringVisual = GetTree().Root.FindChild("Ring", true, false) as RingVisual;
        if (ringVisual != null)
        {
            _grid = ringVisual.Grid;
        }
        else
        {
            GD.PushWarning("CitizenManager: RingVisual not found. Room visits will be disabled.");
        }

        // Spawn 5 starter citizens evenly spaced around the ring
        SpawnStarterCitizens(5);

        // Update EconomyManager with initial citizen count for income calculation
        EconomyManager.Instance?.SetCitizenCount(_citizens.Count);
    }

    // -------------------------------------------------------------------------
    // Spawning
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spawns the initial set of citizens evenly distributed around the walkway ring.
    /// Each citizen gets random body type, random colors from the curated palette,
    /// and a sequential name from CitizenNames.
    /// </summary>
    /// <param name="count">Number of starter citizens to spawn.</param>
    private void SpawnStarterCitizens(int count)
    {
        var bodyTypes = System.Enum.GetValues<CitizenData.BodyType>();

        for (int i = 0; i < count; i++)
        {
            // Create CitizenData with random appearance
            var data = new CitizenData
            {
                CitizenName = CitizenNames.GetNextName(),
                Body = bodyTypes[GD.RandRange(0, bodyTypes.Length - 1)],
                PrimaryColor = Palette[GD.RandRange(0, Palette.Length - 1)],
                SecondaryColor = Palette[GD.RandRange(0, Palette.Length - 1)]
            };

            // Ensure secondary color differs from primary
            while (data.SecondaryColor == data.PrimaryColor && Palette.Length > 1)
            {
                data.SecondaryColor = Palette[GD.RandRange(0, Palette.Length - 1)];
            }

            // Create and initialize CitizenNode with grid reference for room visits
            var citizen = new CitizenNode();
            float startAngle = (float)i / count * Mathf.Tau;
            citizen.Initialize(data, startAngle, _grid);

            AddChild(citizen);
            _citizens.Add(citizen);

            // Emit arrival event
            GameEvents.Instance?.EmitCitizenArrived(data.CitizenName);
        }
    }
}
