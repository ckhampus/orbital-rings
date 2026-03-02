using Godot;

namespace OrbitalRings.UI;

/// <summary>
/// Screen-space tooltip that follows the mouse cursor, showing segment
/// position and occupancy status (e.g., "Outer 3 -- Empty").
///
/// Lives under a CanvasLayer in the scene tree so it renders above 3D content.
/// SegmentInteraction calls Show/Hide to control visibility and text.
///
/// MouseFilter is set to Ignore on both the panel and label so the tooltip
/// never intercepts clicks intended for the 3D viewport.
/// </summary>
public partial class SegmentTooltip : PanelContainer
{
    private Label _label;

    /// <summary>Pixel offset from cursor to avoid overlapping the pointer.</summary>
    private static readonly Vector2 CursorOffset = new(16, 16);

    public override void _Ready()
    {
        // Create label child programmatically
        _label = new Label();
        AddChild(_label);

        // Prevent tooltip from intercepting mouse events
        MouseFilter = MouseFilterEnum.Ignore;
        _label.MouseFilter = MouseFilterEnum.Ignore;

        // Start hidden
        Visible = false;

        // Style the panel background
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.15f, 0.12f, 0.18f, 0.85f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        AddThemeStyleboxOverride("panel", styleBox);

        // Style the label text
        _label.AddThemeColorOverride("font_color", new Color(0.95f, 0.93f, 0.90f));
        _label.AddThemeFontSizeOverride("font_size", 14);
    }

    /// <summary>
    /// Shows the tooltip with the given text near the mouse position,
    /// clamped to the viewport edges.
    /// </summary>
    public void Show(string text, Vector2 mousePos)
    {
        _label.Text = text;
        Visible = true;

        // Position with offset from cursor, clamped to viewport
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        Vector2 pos = mousePos + CursorOffset;

        // Clamp so tooltip doesn't overflow right/bottom edges
        pos.X = Mathf.Min(pos.X, viewport.X - Size.X - 8);
        pos.Y = Mathf.Min(pos.Y, viewport.Y - Size.Y - 8);

        Position = pos;
    }

    /// <summary>
    /// Hides the tooltip.
    /// </summary>
    public new void Hide()
    {
        Visible = false;
    }
}
