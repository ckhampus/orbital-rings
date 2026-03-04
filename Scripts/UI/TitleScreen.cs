using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Citizens;

namespace OrbitalRings.UI;

/// <summary>
/// Title screen with "Orbital Rings" title, Continue/New Station buttons, and
/// a confirmation dialog for overwriting existing saves.
///
/// Programmatic UI built in _Ready() -- matches established pattern (all UI
/// built in code, no scene-based layout). Dark space background with warm
/// white title text and semi-transparent styled buttons.
///
/// Continue button only appears when a save file exists. New Station shows a
/// confirmation dialog when a save exists to prevent accidental data loss.
///
/// Scene transitions use SaveManager APIs:
///   Continue: ApplyState (pre-scene) -> ChangeSceneToFile -> ScheduleSceneRestore (post-scene)
///   New Station: ClearSave -> reset StateLoaded flags -> ChangeSceneToFile
/// </summary>
public partial class TitleScreen : Control
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private const string GameScenePath = "res://Scenes/QuickTest/QuickTestScene.tscn";

    // -------------------------------------------------------------------------
    // UI references (built in _Ready)
    // -------------------------------------------------------------------------

    private Button _continueButton;
    private PanelContainer _confirmDialog;
    private Label _warningLabel;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Full-screen container
        AnchorsPreset = (int)LayoutPreset.FullRect;

        // 1. Dark space background
        var background = new ColorRect();
        background.Color = new Color(0.05f, 0.05f, 0.1f, 1.0f);
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(background);

        // 2. Title label: "Orbital Rings" centered, ~35% from top
        var title = new Label();
        title.Text = "Orbital Rings";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 48);
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.8f));
        title.SetAnchorsPreset(LayoutPreset.CenterTop);
        title.OffsetTop = 200f;
        title.OffsetLeft = -300f;
        title.OffsetRight = 300f;
        title.OffsetBottom = 270f;
        AddChild(title);

        // 3. Button container centered, ~50% from top
        var buttonBox = new VBoxContainer();
        buttonBox.SetAnchorsPreset(LayoutPreset.CenterTop);
        buttonBox.OffsetTop = 320f;
        buttonBox.OffsetLeft = -100f;
        buttonBox.OffsetRight = 100f;
        buttonBox.OffsetBottom = 500f;
        buttonBox.AddThemeConstantOverride("separation", 16);
        AddChild(buttonBox);

        // Continue button (only if save exists)
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave();
        if (hasSave)
        {
            _continueButton = CreateStyledButton("Continue");
            _continueButton.Pressed += OnContinuePressed;
            buttonBox.AddChild(_continueButton);
        }

        // New Station button (always visible)
        var newStationButton = CreateStyledButton("New Station");
        newStationButton.Pressed += OnNewStationPressed;
        buttonBox.AddChild(newStationButton);

        // 4. Warning label (hidden, shown on load failure)
        _warningLabel = new Label();
        _warningLabel.Text = "Save file could not be loaded.";
        _warningLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _warningLabel.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.4f));
        _warningLabel.AddThemeFontSizeOverride("font_size", 16);
        _warningLabel.SetAnchorsPreset(LayoutPreset.CenterTop);
        _warningLabel.OffsetTop = 480f;
        _warningLabel.OffsetLeft = -200f;
        _warningLabel.OffsetRight = 200f;
        _warningLabel.OffsetBottom = 510f;
        _warningLabel.Visible = false;
        AddChild(_warningLabel);

        // 5. Confirmation dialog (hidden until needed)
        BuildConfirmDialog();
    }

    // -------------------------------------------------------------------------
    // Confirmation dialog
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds the "Start a new station?" confirmation dialog, initially hidden.
    /// Centered panel with warning text and Confirm/Cancel buttons.
    /// </summary>
    private void BuildConfirmDialog()
    {
        _confirmDialog = new PanelContainer();
        _confirmDialog.SetAnchorsPreset(LayoutPreset.Center);
        _confirmDialog.OffsetLeft = -200f;
        _confirmDialog.OffsetTop = -80f;
        _confirmDialog.OffsetRight = 200f;
        _confirmDialog.OffsetBottom = 80f;
        _confirmDialog.Visible = false;

        // Dark background style
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.08f, 0.08f, 0.14f, 0.95f);
        panelStyle.CornerRadiusTopLeft = 8;
        panelStyle.CornerRadiusTopRight = 8;
        panelStyle.CornerRadiusBottomLeft = 8;
        panelStyle.CornerRadiusBottomRight = 8;
        panelStyle.ContentMarginLeft = 20;
        panelStyle.ContentMarginRight = 20;
        panelStyle.ContentMarginTop = 20;
        panelStyle.ContentMarginBottom = 20;
        _confirmDialog.AddThemeStyleboxOverride("panel", panelStyle);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 16);

        var promptLabel = new Label();
        promptLabel.Text = "Start a new station?\nYour current station will be lost.";
        promptLabel.HorizontalAlignment = HorizontalAlignment.Center;
        promptLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.85f, 0.8f));
        promptLabel.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(promptLabel);

        var buttonRow = new HBoxContainer();
        buttonRow.Alignment = BoxContainer.AlignmentMode.Center;
        buttonRow.AddThemeConstantOverride("separation", 24);

        var confirmBtn = CreateStyledButton("Confirm");
        confirmBtn.CustomMinimumSize = new Vector2(120, 40);
        confirmBtn.Pressed += () =>
        {
            _confirmDialog.Visible = false;
            StartNewStation();
        };
        buttonRow.AddChild(confirmBtn);

        var cancelBtn = CreateStyledButton("Cancel");
        cancelBtn.CustomMinimumSize = new Vector2(120, 40);
        cancelBtn.Pressed += () => _confirmDialog.Visible = false;
        buttonRow.AddChild(cancelBtn);

        vbox.AddChild(buttonRow);
        _confirmDialog.AddChild(vbox);
        AddChild(_confirmDialog);
    }

    // -------------------------------------------------------------------------
    // Button handlers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads saved game state and transitions to the game scene.
    /// ApplyState restores Autoload state before scene change.
    /// PendingLoad + ScheduleSceneRestore handle scene-dependent state after load.
    /// </summary>
    private void OnContinuePressed()
    {
        var saveData = SaveManager.Instance.Load();

        if (saveData == null)
        {
            _warningLabel.Visible = true;
            return;
        }

        SaveManager.Instance.ApplyState(saveData);
        SaveManager.Instance.PendingLoad = saveData;
        GetTree().ChangeSceneToFile(GameScenePath);
        SaveManager.Instance.ScheduleSceneRestore();
    }

    /// <summary>
    /// Shows confirmation dialog if a save exists, otherwise starts fresh directly.
    /// Prevents accidental data loss from misclicks.
    /// </summary>
    private void OnNewStationPressed()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
        {
            _confirmDialog.Visible = true;
        }
        else
        {
            StartNewStation();
        }
    }

    /// <summary>
    /// Clears save data, resets all StateLoaded flags so managers initialize fresh,
    /// and transitions to the game scene.
    /// </summary>
    private void StartNewStation()
    {
        SaveManager.Instance?.ClearSave();

        // Reset StateLoaded flags so managers initialize with defaults
        CitizenManager.StateLoaded = false;
        HappinessManager.StateLoaded = false;
        EconomyManager.StateLoaded = false;

        GetTree().ChangeSceneToFile(GameScenePath);
    }

    // -------------------------------------------------------------------------
    // Shared button styling
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a Button with dark semi-transparent StyleBoxFlat styling,
    /// matching the established HUD pattern (MuteToggle, BuildPanel).
    /// </summary>
    private static Button CreateStyledButton(string text)
    {
        var button = new Button();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(200, 50);

        var styleNormal = new StyleBoxFlat();
        styleNormal.BgColor = new Color(0.15f, 0.15f, 0.2f, 0.7f);
        styleNormal.CornerRadiusTopLeft = 4;
        styleNormal.CornerRadiusTopRight = 4;
        styleNormal.CornerRadiusBottomLeft = 4;
        styleNormal.CornerRadiusBottomRight = 4;

        var styleHover = new StyleBoxFlat();
        styleHover.BgColor = new Color(0.2f, 0.2f, 0.28f, 0.8f);
        styleHover.CornerRadiusTopLeft = 4;
        styleHover.CornerRadiusTopRight = 4;
        styleHover.CornerRadiusBottomLeft = 4;
        styleHover.CornerRadiusBottomRight = 4;

        var stylePressed = new StyleBoxFlat();
        stylePressed.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        stylePressed.CornerRadiusTopLeft = 4;
        stylePressed.CornerRadiusTopRight = 4;
        stylePressed.CornerRadiusBottomLeft = 4;
        stylePressed.CornerRadiusBottomRight = 4;

        button.AddThemeStyleboxOverride("normal", styleNormal);
        button.AddThemeStyleboxOverride("hover", styleHover);
        button.AddThemeStyleboxOverride("pressed", stylePressed);

        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", new Color(0.7f, 0.7f, 0.7f));
        button.AddThemeFontSizeOverride("font_size", 20);

        return button;
    }
}
