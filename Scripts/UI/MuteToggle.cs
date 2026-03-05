using Godot;

namespace OrbitalRings.UI;

/// <summary>
/// Keyboard-only mute toggle that silences all audio via the master bus.
/// Responds to the M key press -- no visible UI element.
///
/// Unmuting restores audio to its previous state (AudioServer bus mute
/// preserves volume levels and per-bus settings).
/// </summary>
public partial class MuteToggle : Node
{
    private bool _muted;

    // -------------------------------------------------------------------------
    // Keyboard shortcut (M key)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Toggles mute state on M key press. Directly mutes/unmutes the
    /// master audio bus via AudioServer.
    /// </summary>
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.M)
        {
            _muted = !_muted;
            int masterBus = AudioServer.GetBusIndex("Master");
            AudioServer.SetBusMute(masterBus, _muted);
        }
    }
}
