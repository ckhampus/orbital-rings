using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Build;
using OrbitalRings.Citizens;
using OrbitalRings.Data;
using OrbitalRings.Ring;

namespace OrbitalRings.UI;

/// <summary>
/// Screen-space floating popup showing citizen name and wish status.
/// Appears when a citizen is clicked in Normal mode, positioned near the citizen
/// in screen space. Follows the SegmentTooltip pattern (programmatic UI, no .tscn).
///
/// Shows active wish text from WishBoard with category-colored label, or "No wish" (muted)
/// when citizen has no active wish. Text variant selection is deterministic per citizen name.
/// MouseFilter set to Ignore so the panel never intercepts clicks.
/// </summary>
public partial class CitizenInfoPanel : PanelContainer
{
    private Label _nameLabel;
    private Label _homeLabel;
    private Label _categoryLabel;
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

        // Home label (shows room name + location, or "No home")
        _homeLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _homeLabel.AddThemeColorOverride("font_color", new Color(0.68f, 0.66f, 0.62f));
        _homeLabel.AddThemeFontSizeOverride("font_size", 12);
        vbox.AddChild(_homeLabel);

        // Category label (small, colored by category -- hidden when no wish)
        _categoryLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false
        };
        _categoryLabel.AddThemeFontSizeOverride("font_size", 11);
        vbox.AddChild(_categoryLabel);

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
    /// Displays citizen name, wish text from WishBoard, and category label.
    /// </summary>
    /// <param name="citizen">The citizen to show info for.</param>
    /// <param name="screenPos">Mouse position for initial positioning fallback.</param>
    public void ShowForCitizen(CitizenNode citizen, Vector2 screenPos)
    {
        _nameLabel.Text = citizen.Data.CitizenName;

        // Update home label from HousingManager
        var homeAnchor = HousingManager.Instance?.GetHomeForCitizen(citizen.Data.CitizenName);
        if (homeAnchor != null)
        {
            var roomInfo = BuildManager.Instance?.GetPlacedRoom(homeAnchor.Value);
            if (roomInfo != null)
            {
                var (homeRow, homePos) = SegmentGrid.FromIndex(roomInfo.Value.AnchorIndex);
                string rowName = homeRow == SegmentRow.Outer ? "Outer" : "Inner";
                _homeLabel.Text = $"{roomInfo.Value.Definition.RoomName} ({rowName} {homePos + 1})";
            }
            else
            {
                _homeLabel.Text = "No home";
            }
        }
        else
        {
            _homeLabel.Text = "No home";
        }

        // Display wish text from citizen's active wish (direct property access)
        var wish = citizen.CurrentWish;
        if (wish != null && wish.TextVariants.Length > 0)
        {
            // Pick a consistent text variant based on citizen name hash
            // (same citizen always shows same text for same wish)
            int variantIndex = Mathf.Abs(citizen.Data.CitizenName.GetHashCode()) % wish.TextVariants.Length;
            _wishLabel.Text = wish.TextVariants[variantIndex];
            _wishLabel.AddThemeColorOverride("font_color", new Color(0.85f, 0.83f, 0.78f));

            // Show category with accent color
            _categoryLabel.Text = wish.Category.ToString();
            _categoryLabel.AddThemeColorOverride("font_color", GetCategoryColor(wish.Category));
            _categoryLabel.Visible = true;
        }
        else
        {
            _wishLabel.Text = "No wish";
            _wishLabel.AddThemeColorOverride("font_color", new Color(0.65f, 0.63f, 0.60f));
            _categoryLabel.Visible = false;
        }

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

    /// <summary>
    /// Returns an accent color for the wish category matching the icon color palette.
    /// </summary>
    private static Color GetCategoryColor(WishTemplate.WishCategory category)
    {
        return category switch
        {
            WishTemplate.WishCategory.Social => new Color(0.95f, 0.59f, 0.48f),     // coral
            WishTemplate.WishCategory.Comfort => new Color(0.48f, 0.66f, 0.95f),     // soft blue
            WishTemplate.WishCategory.Curiosity => new Color(0.95f, 0.79f, 0.30f),   // amber
            WishTemplate.WishCategory.Variety => new Color(0.44f, 0.81f, 0.59f),     // soft green
            _ => new Color(0.65f, 0.63f, 0.60f)
        };
    }
}
