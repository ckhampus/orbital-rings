using System;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Citizens;

namespace OrbitalRings.UI;

/// <summary>
/// Housing capacity display for the HUD.
/// Shows a smiley icon + "housed/capacity" count (e.g. "5/7"), with a brief
/// scale-up "tick" animation on citizen arrival and room placement/demolition.
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
    private Action<string, int> _onRoomPlaced;
    private Action<int> _onRoomDemolished;

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

        // Subscribe to citizen arrival and room change events
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.CitizenArrived += OnCitizenArrived;

            _onRoomPlaced = (_, _) => { UpdateDisplay(); PlayTickAnimation(); };
            _onRoomDemolished = (_) => { UpdateDisplay(); PlayTickAnimation(); };
            GameEvents.Instance.RoomPlaced += _onRoomPlaced;
            GameEvents.Instance.RoomDemolished += _onRoomDemolished;
        }

        // Initialize housed/capacity display from current state
        UpdateDisplay();
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.CitizenArrived -= OnCitizenArrived;
            if (_onRoomPlaced != null)
                GameEvents.Instance.RoomPlaced -= _onRoomPlaced;
            if (_onRoomDemolished != null)
                GameEvents.Instance.RoomDemolished -= _onRoomDemolished;
        }
    }

    // -------------------------------------------------------------------------
    // Event Handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates the housed/capacity display and plays tick animation
    /// when a new citizen arrives (which may change housed count).
    /// </summary>
    private void OnCitizenArrived(string citizenName)
    {
        UpdateDisplay();
        PlayTickAnimation();
    }

    /// <summary>
    /// Updates the count label text to show "housed/capacity" format
    /// from HousingManager (the authoritative source for housing state).
    /// </summary>
    private void UpdateDisplay()
    {
        int total = CitizenManager.Instance?.CitizenCount ?? 0;
        int capacity = HousingManager.Instance?.TotalCapacity ?? 0;
        _countLabel.Text = $"{total}/{capacity}";
    }

    /// <summary>
    /// Plays a brief scale-up "tick" animation on the count label
    /// for a satisfying visual response to state changes.
    /// </summary>
    private void PlayTickAnimation()
    {
        _activeTween?.Kill();
        _activeTween = _countLabel.CreateTween();
        _countLabel.Scale = new Vector2(1.2f, 1.2f);
        _activeTween.TweenProperty(_countLabel, "scale", new Vector2(1.0f, 1.0f), 0.3f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Elastic);
    }
}
