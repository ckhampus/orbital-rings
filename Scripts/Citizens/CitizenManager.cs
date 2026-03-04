using System.Collections.Generic;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Build;
using OrbitalRings.Core;
using OrbitalRings.Data;
using OrbitalRings.Ring;
using OrbitalRings.UI;

namespace OrbitalRings.Citizens;

/// <summary>
/// Autoload singleton managing citizen lifecycle: spawning, tracking, and removal.
/// Spawns 5 starter citizens at game start with distinct appearances, evenly spaced
/// around the walkway ring. Updates EconomyManager with citizen count for income calculation.
///
/// Handles click detection via ray-plane intersection and polar proximity,
/// showing CitizenInfoPanel with emission glow on selected citizen.
/// Build mode suppresses citizen clicks (locked decision).
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

    /// <summary>
    /// When true, _Ready() skips SpawnStarterCitizens. Set by SaveManager before
    /// scene transition so loaded state is not overwritten by default initialization.
    /// </summary>
    public static bool StateLoaded { get; set; }

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
    // Click detection and selection
    // -------------------------------------------------------------------------

    private Camera3D _camera;
    private readonly Plane _ringPlane = new(Vector3.Up, 0);
    private CitizenNode _selectedCitizen;
    private CitizenInfoPanel _infoPanel;

    /// <summary>
    /// Click proximity threshold in world units.
    /// Generous for small capsules to ensure comfortable click targets.
    /// </summary>
    private const float ClickProximityThreshold = 0.8f;

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
        // (skipped when loading from save -- SaveManager sets StateLoaded before scene transition)
        if (!StateLoaded)
            SpawnStarterCitizens(5);

        // Cache camera reference for click detection ray casting
        _camera = GetViewport().GetCamera3D();

        // Create CitizenInfoPanel on a CanvasLayer (same layer as tooltip)
        var canvasLayer = new CanvasLayer { Layer = 10 };
        AddChild(canvasLayer);
        _infoPanel = new CitizenInfoPanel();
        canvasLayer.AddChild(_infoPanel);
    }

    // -------------------------------------------------------------------------
    // Input handling (click detection)
    // -------------------------------------------------------------------------

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton
            && mouseButton.ButtonIndex == MouseButton.Left
            && mouseButton.Pressed)
        {
            HandleClick(mouseButton.Position);
        }
        else if (@event is InputEventKey keyEvent
            && keyEvent.Pressed
            && keyEvent.Keycode == Key.Escape
            && _selectedCitizen != null)
        {
            DeselectCitizen();
        }
    }

    // -------------------------------------------------------------------------
    // Process (auto-deselect visiting citizens)
    // -------------------------------------------------------------------------

    public override void _Process(double delta)
    {
        // If selected citizen starts visiting (invisible), auto-deselect
        // to prevent glow on invisible citizen
        if (_selectedCitizen != null && _selectedCitizen.IsVisiting)
        {
            DeselectCitizen();
        }
    }

    // -------------------------------------------------------------------------
    // Click handling
    // -------------------------------------------------------------------------

    /// <summary>
    /// Handles a left click: finds citizen at screen position, selects/deselects.
    /// Build mode suppresses citizen clicks (locked decision).
    /// </summary>
    private void HandleClick(Vector2 mousePos)
    {
        // Build mode takes priority -- citizens NOT clickable during Placing or Demolish
        if (BuildManager.Instance?.CurrentMode != BuildMode.Normal) return;

        var found = FindCitizenAtScreenPos(mousePos);

        if (found != null && found != _selectedCitizen)
        {
            // Select new citizen (deselect previous if any)
            DeselectCitizen();
            SelectCitizen(found, mousePos);
        }
        else if (found == null && _selectedCitizen != null)
        {
            // Click-away dismissal
            DeselectCitizen();
        }
    }

    /// <summary>
    /// Finds the closest citizen to a screen position using ray-plane intersection
    /// and XZ proximity testing. Same pattern as SegmentInteraction polar picking.
    /// </summary>
    /// <param name="mousePos">Screen-space mouse position.</param>
    /// <returns>Closest citizen within threshold, or null.</returns>
    private CitizenNode FindCitizenAtScreenPos(Vector2 mousePos)
    {
        if (_camera == null)
        {
            _camera = GetViewport().GetCamera3D();
            if (_camera == null) return null;
        }

        Vector3 rayOrigin = _camera.ProjectRayOrigin(mousePos);
        Vector3 rayDir = _camera.ProjectRayNormal(mousePos);
        Vector3? hit = _ringPlane.IntersectsRay(rayOrigin, rayDir);
        if (hit == null) return null;

        Vector3 hitPoint = hit.Value;
        CitizenNode closest = null;
        float closestDist = float.MaxValue;

        for (int i = 0; i < _citizens.Count; i++)
        {
            var citizen = _citizens[i];

            // Skip invisible/visiting citizens (pitfall #6)
            if (citizen.IsVisiting) continue;

            // XZ distance between hit point and citizen position
            float dx = hitPoint.X - citizen.Position.X;
            float dz = hitPoint.Z - citizen.Position.Z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);

            if (dist < ClickProximityThreshold && dist < closestDist)
            {
                closestDist = dist;
                closest = citizen;
            }
        }

        return closest;
    }

    // -------------------------------------------------------------------------
    // Selection management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Selects a citizen: enables emission glow, shows info panel, emits event.
    /// </summary>
    private void SelectCitizen(CitizenNode citizen, Vector2 mousePos)
    {
        _selectedCitizen = citizen;

        // Enable emission glow on primary mesh material
        // Environment already has glow_enabled=true -- emission > 1.0 triggers bloom automatically
        var mesh = citizen.GetPrimaryMesh();
        if (mesh?.MaterialOverride is StandardMaterial3D mat)
        {
            mat.EmissionEnabled = true;
            mat.Emission = citizen.Data.PrimaryColor.Lightened(0.3f);
            mat.EmissionEnergyMultiplier = 2.5f;
        }

        // Show info panel
        _infoPanel.ShowForCitizen(citizen, mousePos);

        // Emit click event
        GameEvents.Instance?.EmitCitizenClicked(citizen.Data.CitizenName);
    }

    /// <summary>
    /// Deselects the currently selected citizen: removes emission glow, hides info panel.
    /// </summary>
    private void DeselectCitizen()
    {
        if (_selectedCitizen == null) return;

        // Remove emission glow
        var mesh = _selectedCitizen.GetPrimaryMesh();
        if (mesh?.MaterialOverride is StandardMaterial3D mat)
        {
            mat.EmissionEnabled = false;
        }

        // Hide info panel
        _infoPanel.Hide();
        _selectedCitizen = null;
    }

    // -------------------------------------------------------------------------
    // Spawning
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spawns a single citizen at a random (or specified) walkway position.
    /// Creates random appearance from curated palette, adds to scene tree,
    /// updates economy, and fires CitizenArrived event.
    ///
    /// Called by HappinessManager for dynamic arrivals and by SpawnStarterCitizens
    /// for initial population.
    /// </summary>
    /// <param name="startAngle">
    /// Optional angle on the walkway ring (radians). If null, uses a random position.
    /// </param>
    /// <returns>The spawned CitizenNode (caller can apply fade-in tween).</returns>
    public CitizenNode SpawnCitizen(float? startAngle = null)
    {
        var bodyTypes = System.Enum.GetValues<CitizenData.BodyType>();

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

        var citizen = new CitizenNode();
        float angle = startAngle ?? GD.Randf() * Mathf.Tau;
        citizen.Initialize(data, angle, _grid);

        AddChild(citizen);
        _citizens.Add(citizen);

        EconomyManager.Instance?.SetCitizenCount(_citizens.Count);
        GameEvents.Instance?.EmitCitizenArrived(data.CitizenName);

        return citizen;
    }

    /// <summary>
    /// Spawns the initial set of citizens evenly distributed around the walkway ring.
    /// </summary>
    /// <param name="count">Number of starter citizens to spawn.</param>
    private void SpawnStarterCitizens(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = (float)i / count * Mathf.Tau;
            SpawnCitizen(angle);
        }
    }

    // -------------------------------------------------------------------------
    // Save/Load API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Removes all active citizens, frees their nodes, clears the list,
    /// and resets the economy citizen count. Used before loading saved state.
    /// </summary>
    public void ClearCitizens()
    {
        foreach (var citizen in _citizens)
        {
            if (IsInstanceValid(citizen))
                citizen.QueueFree();
        }
        _citizens.Clear();
        EconomyManager.Instance?.SetCitizenCount(0);
    }

    /// <summary>
    /// Spawns a citizen with exact saved state. Does NOT emit CitizenArrived
    /// (they already arrived in the original session).
    /// </summary>
    public CitizenNode SpawnCitizenFromSave(
        string name, int bodyType,
        float primaryR, float primaryG, float primaryB,
        float secondaryR, float secondaryG, float secondaryB,
        float angle, float direction, string wishId)
    {
        var data = new CitizenData
        {
            CitizenName = name,
            Body = (CitizenData.BodyType)bodyType,
            PrimaryColor = new Color(primaryR, primaryG, primaryB),
            SecondaryColor = new Color(secondaryR, secondaryG, secondaryB)
        };

        var citizen = new CitizenNode();
        citizen.Initialize(data, angle, _grid);
        citizen.SetDirection(direction);

        AddChild(citizen);
        _citizens.Add(citizen);

        EconomyManager.Instance?.SetCitizenCount(_citizens.Count);

        // Restore active wish if citizen had one
        if (!string.IsNullOrEmpty(wishId))
        {
            citizen.SetWishFromSave(wishId);
        }

        return citizen;
    }
}
