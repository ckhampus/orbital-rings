using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Citizens;

namespace OrbitalRings.UI;

/// <summary>
/// Citizen icon and population count display for the HUD.
/// Shows a smiley icon + current citizen count, with a brief scale-up
/// "tick" animation on each new citizen arrival.
///
/// Builds all child nodes programmatically in _Ready(), following the CreditHUD pattern.
/// Anchored top-right in HUDLayer (CanvasLayer layer 5) via QuickTestScene.tscn.
///
/// Layout position: middle in the top-right cluster (credits | population | happiness bar).
/// </summary>
public partial class PopulationDisplay : MarginContainer
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>Smiley icon color: mint/teal for cozy feel.</summary>
    private static readonly Color IconColor = new(0.5f, 0.85f, 0.75f);

    /// <summary>Count text color: warm white matching CreditHUD.</summary>
    private static readonly Color CountColor = new(0.95f, 0.93f, 0.90f);

    // -------------------------------------------------------------------------
    // UI Nodes
    // -------------------------------------------------------------------------

    private HBoxContainer _hbox;
    private Label _iconLabel;
    private Label _countLabel;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private Tween _activeTween;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Build UI hierarchy programmatically
        _hbox = new HBoxContainer();
        _hbox.AddThemeConstantOverride("separation", 4);
        _hbox.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_hbox);

        // Citizen icon (smiley for cozy feel)
        _iconLabel = new Label();
        _iconLabel.Text = "\u263A"; // Unicode smiley face
        _iconLabel.AddThemeColorOverride("font_color", IconColor);
        _iconLabel.AddThemeFontSizeOverride("font_size", 20);
        _iconLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_iconLabel);

        // Population count
        _countLabel = new Label();
        _countLabel.AddThemeColorOverride("font_color", CountColor);
        _countLabel.AddThemeFontSizeOverride("font_size", 20);
        _countLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_countLabel);

        // Subscribe to citizen arrival events
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.CitizenArrived += OnCitizenArrived;
        }

        // Initialize count from current state
        int initialCount = CitizenManager.Instance?.CitizenCount ?? 0;
        _countLabel.Text = initialCount.ToString();
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.CitizenArrived -= OnCitizenArrived;
        }
    }

    // -------------------------------------------------------------------------
    // Event Handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates the count label and plays a brief scale-up "tick" animation
    /// when a new citizen arrives.
    /// </summary>
    private void OnCitizenArrived(string citizenName)
    {
        // Update count from CitizenManager (authoritative source)
        int count = CitizenManager.Instance?.CitizenCount ?? 0;
        _countLabel.Text = count.ToString();

        // Kill-before-create pattern for the tick animation
        _activeTween?.Kill();

        // Brief scale-up animation on the count label for a satisfying "tick" feel
        _activeTween = _countLabel.CreateTween();
        _countLabel.Scale = new Vector2(1.2f, 1.2f);
        _activeTween.TweenProperty(_countLabel, "scale", new Vector2(1.0f, 1.0f), 0.3f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);
    }
}
