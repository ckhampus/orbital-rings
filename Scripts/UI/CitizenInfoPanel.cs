using Godot;
using OrbitalRings.Citizens;

namespace OrbitalRings.UI;

/// <summary>
/// Screen-space floating popup showing citizen name and wish status.
/// Appears when a citizen is clicked in Normal mode, positioned near the citizen
/// in screen space. Follows the SegmentTooltip pattern (programmatic UI, no .tscn).
///
/// For v1, wish text is always "No wish" (placeholder for Phase 6).
/// MouseFilter set to Ignore so the panel never intercepts clicks.
/// </summary>
public partial class CitizenInfoPanel : PanelContainer
{
    private Label _nameLabel;
    private Label _wishLabel;

    public override void _Ready()
    {
        Visible = false;

        // Prevent panel from intercepting mouse events (same as SegmentTooltip)
        MouseFilter = MouseFilterEnum.Ignore;

        // Style the panel background: dark semi-transparent, rounded corners
        var styleBox = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.15f, 0.85f),
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        AddThemeStyleboxOverride("panel", styleBox);

        // Layout container
        var vbox = new VBoxContainer();
        AddChild(vbox);

        // Name label (bold, slightly larger)
        _nameLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _nameLabel.AddThemeColorOverride("font_color", new Color(0.95f, 0.93f, 0.90f));
        _nameLabel.AddThemeFontSizeOverride("font_size", 16);
        vbox.AddChild(_nameLabel);

        // Wish label (smaller, muted color)
        _wishLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _wishLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.63f, 0.60f));
        _wishLabel.AddThemeFontSizeOverride("font_size", 13);
        vbox.AddChild(_wishLabel);

        // Minimum size for consistent appearance
        CustomMinimumSize = new Vector2(140, 50);
    }

    /// <summary>
    /// Shows the panel near the given citizen's screen-space position.
    /// Displays citizen name and wish placeholder.
    /// </summary>
    /// <param name="citizen">The citizen to show info for.</param>
    /// <param name="screenPos">Mouse position for initial positioning fallback.</param>
    public void ShowForCitizen(CitizenNode citizen, Vector2 screenPos)
    {
        _nameLabel.Text = citizen.Data.CitizenName;
        _wishLabel.Text = "No wish";  // Placeholder for Phase 6 wish display

        // Position above and to the right of the citizen in screen space
        var camera = GetViewport().GetCamera3D();
        if (camera != null)
        {
            Vector2 citizenScreenPos = camera.UnprojectPosition(citizen.GlobalPosition);
            Position = citizenScreenPos + new Vector2(20, -60);
        }
        else
        {
            Position = screenPos + new Vector2(20, -60);
        }

        // Clamp to viewport bounds (follow SegmentTooltip clamping pattern)
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        Vector2 pos = Position;
        pos.X = Mathf.Min(pos.X, viewport.X - Size.X - 8);
        pos.Y = Mathf.Max(pos.Y, 8);
        pos.X = Mathf.Max(pos.X, 8);
        pos.Y = Mathf.Min(pos.Y, viewport.Y - Size.Y - 8);
        Position = pos;

        Visible = true;
    }

    /// <summary>
    /// Hides the citizen info panel.
    /// </summary>
    public new void Hide()
    {
        Visible = false;
    }
}
