using Godot;
using OrbitalRings.Autoloads;

namespace OrbitalRings.UI;

/// <summary>
/// Horizontal fill bar with percentage label showing station happiness.
/// Tweens smoothly on GameEvents.HappinessChanged with a warm pulse effect,
/// and spawns floating "+X%" text on each happiness gain.
///
/// Builds all child nodes programmatically in _Ready(), following the CreditHUD pattern.
/// Anchored top-right in HUDLayer (CanvasLayer layer 5) via QuickTestScene.tscn.
///
/// Layout position: rightmost in the top-right cluster (credits | population | happiness bar).
/// </summary>
public partial class HappinessBar : MarginContainer
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const float MaxBarWidth = 120f;
    private const float BarHeight = 16f;

    /// <summary>Bar background: dark, matches tooltip aesthetic.</summary>
    private static readonly Color BarBgColor = new(0.15f, 0.12f, 0.18f, 0.8f);

    /// <summary>Bar fill at low happiness: warm coral.</summary>
    private static readonly Color LowHappinessColor = new(0.95f, 0.55f, 0.45f);

    /// <summary>Bar fill at high happiness: warm gold.</summary>
    private static readonly Color HighHappinessColor = new(1.0f, 0.85f, 0.4f);

    /// <summary>Floating text color: warm gold for "+X%".</summary>
    private static readonly Color FloatingTextColor = new(1.0f, 0.9f, 0.4f);

    /// <summary>Heart icon color: warm pink/coral.</summary>
    private static readonly Color HeartColor = new(0.95f, 0.55f, 0.55f);

    // -------------------------------------------------------------------------
    // UI Nodes
    // -------------------------------------------------------------------------

    private HBoxContainer _hbox;
    private Label _heartLabel;
    private PanelContainer _barBackground;
    private ColorRect _barFill;
    private Label _percentLabel;

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private float _previousHappiness;
    private Tween _activeTween;

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
        _heartLabel.AddThemeFontSizeOverride("font_size", 18);
        _heartLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_heartLabel);

        // Bar background (PanelContainer with dark style)
        _barBackground = new PanelContainer();
        _barBackground.CustomMinimumSize = new Vector2(MaxBarWidth, BarHeight);
        _barBackground.MouseFilter = MouseFilterEnum.Ignore;

        var bgStyle = new StyleBoxFlat
        {
            BgColor = BarBgColor,
            CornerRadiusTopLeft = 3,
            CornerRadiusTopRight = 3,
            CornerRadiusBottomLeft = 3,
            CornerRadiusBottomRight = 3
        };
        _barBackground.AddThemeStyleboxOverride("panel", bgStyle);
        _hbox.AddChild(_barBackground);

        // Bar fill (ColorRect inside the background)
        _barFill = new ColorRect();
        _barFill.Color = LowHappinessColor;
        _barFill.CustomMinimumSize = new Vector2(0, BarHeight);
        _barFill.Size = new Vector2(0, BarHeight);
        _barFill.MouseFilter = MouseFilterEnum.Ignore;
        _barBackground.AddChild(_barFill);

        // Percentage label
        _percentLabel = new Label();
        _percentLabel.Text = "0%";
        _percentLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.93f, 0.90f));
        _percentLabel.AddThemeFontSizeOverride("font_size", 14);
        _percentLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_percentLabel);

        // Subscribe to happiness changes
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.HappinessChanged += OnHappinessChanged;
        }

        // Initialize from current happiness value
        float initialHappiness = HappinessManager.Instance?.Happiness ?? 0f;
        _previousHappiness = initialHappiness;
        UpdateBarImmediate(initialHappiness);
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.HappinessChanged -= OnHappinessChanged;
        }
    }

    // -------------------------------------------------------------------------
    // Event Handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Smoothly tweens the bar fill, color, and percentage on happiness change.
    /// Spawns floating "+X%" text and applies a brief pulse glow.
    /// </summary>
    private void OnHappinessChanged(float newHappiness)
    {
        // Kill existing tween (kill-before-create pattern)
        _activeTween?.Kill();

        // Calculate floating text delta before updating _previousHappiness
        float delta = newHappiness - _previousHappiness;
        _previousHappiness = newHappiness;

        // Target fill width and color
        float targetWidth = newHappiness * MaxBarWidth;
        Color targetColor = LowHappinessColor.Lerp(HighHappinessColor, newHappiness);
        string percentText = $"{Mathf.RoundToInt(newHappiness * 100)}%";

        // Create animated tween
        _activeTween = CreateTween();
        _activeTween.SetParallel(true);

        // Tween fill width
        _activeTween.TweenProperty(_barFill, "custom_minimum_size:x", targetWidth, 0.5f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Cubic);

        // Tween fill color
        _activeTween.TweenProperty(_barFill, "color", targetColor, 0.5f)
            .SetEase(Tween.EaseType.Out);

        _activeTween.SetParallel(false);

        // Update percentage label after tween completes
        _activeTween.TweenCallback(Callable.From(() =>
        {
            _percentLabel.Text = percentText;
        }));

        // Update percentage immediately for responsiveness
        _percentLabel.Text = percentText;

        // Pulse effect: briefly brighten the fill bar's modulate
        var pulseTween = _barFill.CreateTween();
        pulseTween.TweenProperty(_barFill, "modulate",
            new Color(1.3f, 1.3f, 1.3f, 1.0f), 0.15f)
            .SetEase(Tween.EaseType.Out);
        pulseTween.TweenProperty(_barFill, "modulate",
            new Color(1.0f, 1.0f, 1.0f, 1.0f), 0.35f)
            .SetEase(Tween.EaseType.In);

        // Spawn floating "+X%" text if there was a gain
        if (delta > 0.001f)
        {
            SpawnFloatingText(delta);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets bar state immediately without animation (used for initialization).
    /// </summary>
    private void UpdateBarImmediate(float happiness)
    {
        float targetWidth = happiness * MaxBarWidth;
        Color targetColor = LowHappinessColor.Lerp(HighHappinessColor, happiness);

        _barFill.CustomMinimumSize = new Vector2(targetWidth, BarHeight);
        _barFill.Size = new Vector2(targetWidth, BarHeight);
        _barFill.Color = targetColor;
        _percentLabel.Text = $"{Mathf.RoundToInt(happiness * 100)}%";
    }

    /// <summary>
    /// Spawns a floating "+X%" text near the happiness bar.
    /// </summary>
    private void SpawnFloatingText(float delta)
    {
        int percentGain = Mathf.RoundToInt(delta * 100);
        if (percentGain <= 0) return;

        var floater = new FloatingText();
        AddChild(floater);

        // Position near the bar, slightly above
        Vector2 startPos = new Vector2(_barBackground.Position.X + MaxBarWidth / 2, -10);
        floater.Setup($"+{percentGain}%", FloatingTextColor, startPos);
    }
}
