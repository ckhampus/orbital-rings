using System.Collections.Generic;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Data;

namespace OrbitalRings.UI;

/// <summary>
/// Station clock HUD widget showing the current period icon and name.
/// Displays a sun icon for Morning/Day and a moon icon for Evening/Night,
/// with period-specific warm colors and a scale pop animation on period change.
///
/// Builds all child nodes programmatically in _Ready(), following the MoodHUD pattern.
/// Anchored top-right in HUDLayer (CanvasLayer layer 5) via QuickTestScene.tscn.
///
/// Layout position: rightmost in the top-right cluster (credits | population | mood | clock).
/// </summary>
public partial class ClockHUD : MarginContainer
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>
    /// Period color dictionary: warm palette with period-appropriate tones.
    /// Used for both icon and period label font colors.
    /// </summary>
    private static readonly Dictionary<StationPeriod, Color> PeriodColors = new()
    {
        { StationPeriod.Morning, new Color(0.95f, 0.85f, 0.45f) },  // soft gold
        { StationPeriod.Day,     new Color(0.95f, 0.93f, 0.90f) },  // warm white
        { StationPeriod.Evening, new Color(0.95f, 0.65f, 0.40f) },  // amber/coral
        { StationPeriod.Night,   new Color(0.55f, 0.70f, 0.90f) },  // soft blue
    };

    // -------------------------------------------------------------------------
    // UI Nodes
    // -------------------------------------------------------------------------

    private HBoxContainer _hbox;
    private Label _iconLabel;
    private Label _periodLabel;

    // -------------------------------------------------------------------------
    // Tween State (kill-before-create)
    // -------------------------------------------------------------------------

    private Tween _popTween;

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

        // Period icon (sun/moon)
        _iconLabel = new Label();
        _iconLabel.AddThemeFontSizeOverride("font_size", 20);
        _iconLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_iconLabel);

        // Period name text
        _periodLabel = new Label();
        _periodLabel.AddThemeFontSizeOverride("font_size", 20);
        _periodLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_periodLabel);

        // Subscribe to period change events
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.PeriodChanged += OnPeriodChanged;
        }

        // Initialize from current state (no animation on startup)
        StationPeriod currentPeriod = StationClock.Instance?.CurrentPeriod ?? StationPeriod.Morning;
        _iconLabel.Text = GetPeriodIcon(currentPeriod);
        _periodLabel.Text = currentPeriod.ToString();

        Color color = PeriodColors.GetValueOrDefault(currentPeriod, new Color(0.95f, 0.93f, 0.90f));
        _iconLabel.AddThemeColorOverride("font_color", color);
        _periodLabel.AddThemeColorOverride("font_color", color);
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.PeriodChanged -= OnPeriodChanged;
        }
    }

    // -------------------------------------------------------------------------
    // Event Handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates the period icon and name label with new period-specific colors,
    /// and plays a scale pop animation on the entire HBox widget.
    /// </summary>
    private void OnPeriodChanged(StationPeriod newPeriod, StationPeriod previousPeriod)
    {
        // Update icon and text
        _iconLabel.Text = GetPeriodIcon(newPeriod);
        _periodLabel.Text = newPeriod.ToString();

        // Update colors from period palette
        Color color = PeriodColors.GetValueOrDefault(newPeriod, new Color(0.95f, 0.93f, 0.90f));
        _iconLabel.AddThemeColorOverride("font_color", color);
        _periodLabel.AddThemeColorOverride("font_color", color);

        // Scale pop animation on the whole widget (1.15x elastic bounce)
        _popTween?.Kill();
        _popTween = _hbox.CreateTween();
        _hbox.Scale = new Vector2(1.15f, 1.15f);
        _popTween.TweenProperty(_hbox, "scale", new Vector2(1.0f, 1.0f), 0.3f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the Unicode icon character for the given station period.
    /// Sun (U+2600) for Morning/Day, Moon (U+263D) for Evening/Night.
    /// </summary>
    private static string GetPeriodIcon(StationPeriod period) => period switch
    {
        StationPeriod.Morning => "\u2600",  // Black sun with rays
        StationPeriod.Day => "\u2600",  // Black sun with rays
        StationPeriod.Evening => "\u263D",  // First quarter moon
        StationPeriod.Night => "\u263D",  // First quarter moon
        _ => "\u2600",
    };
}
