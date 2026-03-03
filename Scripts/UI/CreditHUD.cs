using Godot;
using OrbitalRings.Autoloads;

namespace OrbitalRings.UI;

/// <summary>
/// Credit balance HUD anchored to the top-right corner of the screen.
/// Shows a rolling counter with tween animation, income tick flash,
/// floating +N/-N numbers on income/spend/refund, and an income
/// breakdown tooltip on hover.
///
/// Builds all child nodes programmatically in _Ready(). Manually subscribes
/// to GameEvents in _EnterTree/_ExitTree (cannot extend SafeNode because
/// SafeNode extends Node, not Control).
///
/// CanvasLayer layer 5 ensures it renders above 3D but below TooltipLayer (10).
/// </summary>
public partial class CreditHUD : MarginContainer
{
    // -------------------------------------------------------------------------
    // Colors
    // -------------------------------------------------------------------------

    private static readonly Color IncomeGreen = new(0.3f, 0.85f, 0.3f);
    private static readonly Color SpendRed = new(0.95f, 0.3f, 0.3f);
    private static readonly Color FlashGold = new(1.0f, 0.92f, 0.6f);
    private static readonly Color DefaultWhite = new(0.95f, 0.93f, 0.90f);

    // -------------------------------------------------------------------------
    // UI Nodes
    // -------------------------------------------------------------------------

    private HBoxContainer _hbox;
    private Label _iconLabel;
    private Label _balanceLabel;
    private PanelContainer _tooltipPanel;
    private Label _tooltipLabel;

    // -------------------------------------------------------------------------
    // Rolling Counter State
    // -------------------------------------------------------------------------

    private float _displayedCredits;
    private int _targetCredits;
    private Tween _activeTween;

    // -------------------------------------------------------------------------
    // Floating text offset tracking
    // -------------------------------------------------------------------------

    private float _floatingTextYOffset;
    private static readonly float FloatingTextSpacing = 22f;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Build the UI tree programmatically
        _hbox = new HBoxContainer();
        _hbox.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_hbox);

        _iconLabel = new Label();
        _iconLabel.Text = "\u2605 "; // Unicode star
        _iconLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.85f, 0.3f));
        _iconLabel.AddThemeFontSizeOverride("font_size", 20);
        _iconLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_iconLabel);

        _balanceLabel = new Label();
        _balanceLabel.AddThemeColorOverride("font_color", DefaultWhite);
        _balanceLabel.AddThemeFontSizeOverride("font_size", 20);
        _balanceLabel.MouseFilter = MouseFilterEnum.Ignore;
        _hbox.AddChild(_balanceLabel);

        // Initialize display from current EconomyManager balance
        if (EconomyManager.Instance != null)
        {
            _targetCredits = EconomyManager.Instance.Credits;
            _displayedCredits = _targetCredits;
            _balanceLabel.Text = _targetCredits.ToString("N0");
        }
        else
        {
            _balanceLabel.Text = "0";
        }

        // Connect hover signals on the HBox for tooltip display
        _hbox.MouseEntered += OnMouseEntered;
        _hbox.MouseExited += OnMouseExited;

        // Build the tooltip panel (hidden by default)
        BuildTooltipPanel();
    }

    public override void _EnterTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.CreditsChanged += OnCreditsChanged;
            GameEvents.Instance.IncomeTicked += OnIncomeTicked;
            GameEvents.Instance.CreditsSpent += OnCreditsSpent;
            GameEvents.Instance.CreditsRefunded += OnCreditsRefunded;
        }
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.CreditsChanged -= OnCreditsChanged;
            GameEvents.Instance.IncomeTicked -= OnIncomeTicked;
            GameEvents.Instance.CreditsSpent -= OnCreditsSpent;
            GameEvents.Instance.CreditsRefunded -= OnCreditsRefunded;
        }
    }

    // -------------------------------------------------------------------------
    // Event Handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Rolling counter animation: tweens _displayedCredits toward the new balance.
    /// Kills any existing tween to prevent stacking (research pitfall #4).
    /// </summary>
    private void OnCreditsChanged(int newBalance)
    {
        _targetCredits = newBalance;

        // Kill previous tween to prevent stacking
        _activeTween?.Kill();

        _activeTween = CreateTween();
        _activeTween.TweenMethod(
            Callable.From<float>(UpdateDisplayedCredits),
            _displayedCredits,
            (float)newBalance,
            0.4f
        ).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);

        _activeTween.TweenCallback(Callable.From(() =>
        {
            _displayedCredits = _targetCredits;
            _balanceLabel.Text = _targetCredits.ToString("N0");
        }));
    }

    /// <summary>
    /// Updates the balance label during the rolling counter tween.
    /// </summary>
    private void UpdateDisplayedCredits(float val)
    {
        _displayedCredits = val;
        _balanceLabel.Text = Mathf.RoundToInt(val).ToString("N0");
    }

    /// <summary>
    /// On income tick: flash the balance label warm gold and spawn floating +N text.
    /// No sound per user decision.
    /// </summary>
    private void OnIncomeTicked(int amount)
    {
        // Flash effect: change color to gold, tween back to white
        _balanceLabel.AddThemeColorOverride("font_color", FlashGold);
        var flashTween = CreateTween();
        flashTween.TweenMethod(
            Callable.From<Color>(c => _balanceLabel.AddThemeColorOverride("font_color", c)),
            FlashGold,
            DefaultWhite,
            0.5f
        ).SetEase(Tween.EaseType.Out);

        // Spawn floating text
        SpawnFloatingText($"+{amount}", IncomeGreen);
    }

    /// <summary>
    /// On credits spent: spawn floating -N text in red.
    /// </summary>
    private void OnCreditsSpent(int amount)
    {
        SpawnFloatingText($"-{amount}", SpendRed);
    }

    /// <summary>
    /// On credits refunded: spawn floating +N text in green.
    /// </summary>
    private void OnCreditsRefunded(int amount)
    {
        SpawnFloatingText($"+{amount}", IncomeGreen);
    }

    // -------------------------------------------------------------------------
    // Floating Text
    // -------------------------------------------------------------------------

    /// <summary>
    /// Spawns a FloatingText label near the balance display.
    /// Offsets stacked texts so multiple simultaneous floaters don't overlap.
    /// </summary>
    private void SpawnFloatingText(string text, Color color)
    {
        var floater = new FloatingText();
        AddChild(floater);

        // Position near the balance label, offset downward for stacking
        Vector2 startPos = new Vector2(0, _floatingTextYOffset);
        _floatingTextYOffset += FloatingTextSpacing;

        // Reset offset after a short delay (texts are short-lived)
        var resetTween = CreateTween();
        resetTween.TweenCallback(Callable.From(() => _floatingTextYOffset = 0f))
            .SetDelay(1.2f);

        floater.Setup(text, color, startPos);
    }

    // -------------------------------------------------------------------------
    // Tooltip
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the income breakdown tooltip panel. Hidden by default.
    /// Styled similarly to SegmentTooltip for visual consistency.
    /// </summary>
    private void BuildTooltipPanel()
    {
        _tooltipPanel = new PanelContainer();
        _tooltipPanel.MouseFilter = MouseFilterEnum.Ignore;
        _tooltipPanel.Visible = false;

        // Style to match SegmentTooltip aesthetic
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.12f, 0.18f, 0.9f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        _tooltipPanel.AddThemeStyleboxOverride("panel", styleBox);

        _tooltipLabel = new Label();
        _tooltipLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.93f, 0.90f));
        _tooltipLabel.AddThemeFontSizeOverride("font_size", 13);
        _tooltipLabel.MouseFilter = MouseFilterEnum.Ignore;
        _tooltipPanel.AddChild(_tooltipLabel);

        AddChild(_tooltipPanel);
    }

    /// <summary>
    /// Shows the income breakdown tooltip on mouse enter.
    /// Calls EconomyManager.GetIncomeBreakdown() for current values.
    /// </summary>
    private void OnMouseEntered()
    {
        if (EconomyManager.Instance == null)
            return;

        var (baseIncome, citizenIncome, workBonus, happinessMult, total) =
            EconomyManager.Instance.GetIncomeBreakdown();

        int citizenCount = 0;
        // Show breakdown text
        string breakdownText =
            $"Income per tick:\n" +
            $"  Base station:  +{baseIncome:F0}\n" +
            $"  Citizens:  +{citizenIncome:F0} ({citizenCount} citizens)\n" +
            $"  Work bonus:  +{workBonus:F0}\n" +
            $"  Happiness:  x{happinessMult:F2}\n" +
            $"  \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\n" +
            $"  Total:  +{total} / tick";

        _tooltipLabel.Text = breakdownText;

        // Position below the HBox
        _tooltipPanel.Position = new Vector2(0, _hbox.Size.Y + 4);
        _tooltipPanel.Visible = true;
    }

    /// <summary>
    /// Hides the tooltip on mouse exit.
    /// </summary>
    private void OnMouseExited()
    {
        _tooltipPanel.Visible = false;
    }
}
