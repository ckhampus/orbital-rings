using System.Collections.Generic;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Data;

namespace OrbitalRings.UI;

/// <summary>
/// Combined wish counter + mood tier label HUD widget.
/// Shows a heart icon with lifetime wish count (pulses on wish fulfilled)
/// and the current mood tier name in tier-colored text (cross-fades on tier change).
///
/// Builds all child nodes programmatically in _Ready(), following the CreditHUD/PopulationDisplay pattern.
/// Anchored top-right in HUDLayer (CanvasLayer layer 5) via QuickTestScene.tscn.
///
/// Layout position: rightmost in the top-right cluster (credits | population | mood).
/// </summary>
public partial class MoodHUD : MarginContainer
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>Heart icon color: warm coral, consistent with prior HappinessBar palette.</summary>
    private static readonly Color HeartColor = new(0.95f, 0.55f, 0.55f);

    /// <summary>Count/label text color: warm white matching CreditHUD and PopulationDisplay.</summary>
    private static readonly Color WarmWhite = new(0.95f, 0.93f, 0.90f);

    /// <summary>
    /// Tier color dictionary: warm spectrum from cool-to-warm as mood increases.
    /// Used for tier label font color and floating notification text.
    /// </summary>
    private static readonly Dictionary<MoodTier, Color> TierColors = new()
    {
        { MoodTier.Quiet,   new Color(0.65f, 0.72f, 0.78f) },  // soft grey-blue
        { MoodTier.Cozy,    new Color(0.85f, 0.75f, 0.55f) },  // warm tan/sand
        { MoodTier.Lively,  new Color(0.95f, 0.72f, 0.42f) },  // warm amber
        { MoodTier.Vibrant, new Color(0.95f, 0.60f, 0.40f) },  // warm coral-orange
        { MoodTier.Radiant, new Color(1.00f, 0.85f, 0.35f) },  // bright gold
    };

    // -------------------------------------------------------------------------
    // UI Nodes
    // -------------------------------------------------------------------------

    private HBoxContainer _hbox;
    private Label _heartLabel;
    private Label _countLabel;
    private Label _tierLabel;

    // -------------------------------------------------------------------------
    // Tween State (three independent kill-before-create references)
    // -------------------------------------------------------------------------

    private Tween _pulseTween;
    private Tween _tierColorTween;
    private Tween _tierPopTween;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Build UI hierarchy programmatically
        _hbox = new HBoxContainer();
        _hbox.AddThemeConstantOverride("separation", 6);
        _hbox.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_hbox);

        // Heart icon
        _heartLabel = new Label();
        _heartLabel.Text = "\u2665"; // Unicode heart
        _heartLabel.AddThemeColorOverride("font_color", HeartColor);
        _heartLabel.AddThemeFontSizeOverride("font_size", 20);
        _heartLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_heartLabel);

        // Wish count
        _countLabel = new Label();
        _countLabel.AddThemeColorOverride("font_color", WarmWhite);
        _countLabel.AddThemeFontSizeOverride("font_size", 20);
        _countLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_countLabel);

        // Tier name
        _tierLabel = new Label();
        _tierLabel.AddThemeFontSizeOverride("font_size", 20);
        _tierLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_tierLabel);

        // Subscribe to events
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.WishCountChanged += OnWishCountChanged;
            GameEvents.Instance.MoodTierChanged += OnMoodTierChanged;
        }

        // Initialize from current state (no animation)
        int initialWishes = HappinessManager.Instance?.LifetimeWishes ?? 0;
        _countLabel.Text = initialWishes.ToString();

        MoodTier initialTier = HappinessManager.Instance?.CurrentTier ?? MoodTier.Quiet;
        _tierLabel.Text = initialTier.ToString();
        Color tierColor = TierColors.GetValueOrDefault(initialTier, WarmWhite);
        _tierLabel.AddThemeColorOverride("font_color", tierColor);
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.WishCountChanged -= OnWishCountChanged;
            GameEvents.Instance.MoodTierChanged -= OnMoodTierChanged;
        }
    }

    // -------------------------------------------------------------------------
    // Event Handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates the wish count label and plays a scale bounce pulse animation
    /// on the count label (elastic settle, matching PopulationDisplay pattern).
    /// </summary>
    private void OnWishCountChanged(int newCount)
    {
        _countLabel.Text = newCount.ToString();

        // Kill-before-create pattern for the pulse animation
        _pulseTween?.Kill();
        _pulseTween = _countLabel.CreateTween();
        _countLabel.Scale = new Vector2(1.2f, 1.2f);
        _pulseTween.TweenProperty(_countLabel, "scale", new Vector2(1.0f, 1.0f), 0.3f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);
    }

    /// <summary>
    /// Updates the tier label text and color with cross-fade animation,
    /// plays a scale pop on the tier label, and spawns a floating tier notification.
    /// </summary>
    private void OnMoodTierChanged(MoodTier newTier, MoodTier previousTier)
    {
        // 1. Update tier label text
        _tierLabel.Text = newTier.ToString();

        // 2. Cross-fade tier label color from previous to new tier color
        Color fromColor = TierColors.GetValueOrDefault(previousTier, WarmWhite);
        Color toColor = TierColors.GetValueOrDefault(newTier, WarmWhite);

        _tierColorTween?.Kill();
        _tierColorTween = CreateTween();
        _tierColorTween.TweenMethod(
            Callable.From<Color>(c => _tierLabel.AddThemeColorOverride("font_color", c)),
            fromColor,
            toColor,
            0.3f
        ).SetEase(Tween.EaseType.Out);

        // 3. Scale pop on tier label (slightly smaller than wish pulse for subtlety)
        _tierPopTween?.Kill();
        _tierPopTween = _tierLabel.CreateTween();
        _tierLabel.Scale = new Vector2(1.15f, 1.15f);
        _tierPopTween.TweenProperty(_tierLabel, "scale", new Vector2(1.0f, 1.0f), 0.3f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);

        // 4. Spawn floating tier notification
        SpawnTierNotification(newTier);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spawns a floating "Station mood: X" text above the tier label
    /// using the reusable FloatingText class with tier-appropriate color.
    /// </summary>
    private void SpawnTierNotification(MoodTier tier)
    {
        var floater = new FloatingText();
        AddChild(floater);

        Color tierColor = TierColors.GetValueOrDefault(tier, WarmWhite);
        Vector2 startPos = new Vector2(_tierLabel.Position.X, -20);
        floater.Setup($"Station mood: {tier}", tierColor, startPos);
    }
}
