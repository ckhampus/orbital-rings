using Godot;

namespace OrbitalRings.UI;

/// <summary>
/// Simple mute toggle button that silences all audio via the master bus.
/// Supports both mouse click (toggle mode) and keyboard shortcut (M key).
///
/// Positioned at top-right corner with dark semi-transparent styling
/// matching the established HUD pattern (programmatic UI, no .tscn scene).
///
/// Unmuting restores audio to its previous state (AudioServer bus mute
/// preserves volume levels and per-bus settings).
/// </summary>
public partial class MuteToggle : Button
{
    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Toggle mode: button stays pressed/unpressed on click
        ToggleMode = true;
        ButtonPressed = false; // Starts unmuted

        Text = "Sound: ON";

        // Position: top-right corner
        AnchorRight = 1f;
        AnchorTop = 0f;
        AnchorLeft = 1f;
        AnchorBottom = 0f;
        OffsetLeft = -120f;
        OffsetTop = 10f;
        OffsetRight = -10f;
        OffsetBottom = 40f;

        // Style: dark semi-transparent background (same pattern as BuildPanel buttons)
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

        AddThemeStyleboxOverride("normal", styleNormal);
        AddThemeStyleboxOverride("hover", styleHover);
        AddThemeStyleboxOverride("pressed", stylePressed);

        // Font color: white
        AddThemeColorOverride("font_color", Colors.White);
        AddThemeColorOverride("font_hover_color", Colors.White);
        AddThemeColorOverride("font_pressed_color", new Color(0.7f, 0.7f, 0.7f));

        // Connect toggled signal
        Toggled += OnToggled;
    }

    // -------------------------------------------------------------------------
    // Toggle handler
    // -------------------------------------------------------------------------

    /// <summary>
    /// Mutes or unmutes the master audio bus.
    /// AudioServer.SetBusMute preserves volume levels -- unmuting
    /// restores audio to its previous state automatically.
    /// </summary>
    private void OnToggled(bool muted)
    {
        int masterBus = AudioServer.GetBusIndex("Master");
        AudioServer.SetBusMute(masterBus, muted);
        Text = muted ? "Sound: OFF" : "Sound: ON";
    }

    // -------------------------------------------------------------------------
    // Keyboard shortcut (M key)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Toggles mute state on M key press. ButtonPressed assignment
    /// automatically fires the Toggled signal.
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.M)
        {
            ButtonPressed = !ButtonPressed;
            // Toggled signal fires automatically when ButtonPressed changes
        }
    }
}
