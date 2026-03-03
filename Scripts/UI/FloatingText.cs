using Godot;

namespace OrbitalRings.UI;

/// <summary>
/// Reusable floating text label that drifts upward and fades out, then frees itself.
/// Used by CreditHUD to show +N/-N on income ticks, spend, and refund events.
///
/// Usage: instantiate, call Setup() with text, color, and start position.
/// The label self-destructs after the animation completes (~1.1 seconds).
/// </summary>
public partial class FloatingText : Label
{
    /// <summary>
    /// Configures the label and starts the drift-and-fade animation.
    /// </summary>
    /// <param name="text">Display text, e.g. "+1" or "-50".</param>
    /// <param name="color">Text color (green for income/refund, red for spend).</param>
    /// <param name="startPosition">Initial position in parent coordinates.</param>
    public void Setup(string text, Color color, Vector2 startPosition)
    {
        Text = text;
        Position = startPosition;
        AddThemeColorOverride("font_color", color);
        AddThemeFontSizeOverride("font_size", 18);

        var tween = CreateTween();
        tween.SetParallel(true);

        // Drift upward ~55px over 0.9 seconds with ease-out
        tween.TweenProperty(this, "position:y", startPosition.Y - 55f, 0.9f)
            .SetEase(Tween.EaseType.Out);

        // Fade out with slight delay so text is readable briefly
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.9f)
            .SetEase(Tween.EaseType.In)
            .SetDelay(0.2f);

        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
