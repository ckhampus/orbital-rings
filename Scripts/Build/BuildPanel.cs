using System.Collections.Generic;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Data;

namespace OrbitalRings.Build;

/// <summary>
/// Bottom toolbar UI for the build system. Shows 5 category tabs with room
/// cards and a demolish button. Toggle with B key, close with Escape or
/// clicking away.
///
/// Builds all UI programmatically in _Ready(), following the CreditHUD pattern.
/// Subscribes to GameEvents for build mode changes and economy updates.
///
/// Placed in a CanvasLayer (BuildUILayer, layer 8) in QuickTestScene.tscn.
///
/// Also owns the live cost preview label that tracks the ghost mesh position
/// during placement mode, and the refund preview label during demolish hover.
/// </summary>
public partial class BuildPanel : PanelContainer
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    private static readonly Color PanelBg = new(0.12f, 0.10f, 0.15f, 0.92f);
    private static readonly Color ActiveTabBg = new(0.22f, 0.20f, 0.28f, 0.95f);
    private static readonly Color InactiveTabBg = new(0.16f, 0.14f, 0.20f, 0.7f);
    private static readonly Color DemolishTint = new(0.85f, 0.35f, 0.30f);
    private static readonly Color DemolishActiveBg = new(0.50f, 0.20f, 0.18f, 0.95f);
    private static readonly Color CardBg = new(0.18f, 0.16f, 0.22f, 0.90f);
    private static readonly Color CardHoverBg = new(0.24f, 0.22f, 0.28f, 0.95f);
    private static readonly Color CardSelectedBg = new(0.28f, 0.26f, 0.34f, 1.0f);
    private static readonly Color MutedText = new(0.70f, 0.68f, 0.72f);
    private static readonly Color CostRedText = new(0.95f, 0.30f, 0.30f);
    private static readonly Color RefundGreen = new(0.3f, 0.85f, 0.3f);

    private static readonly string[] CategoryNames = { "1 Housing", "2 Life Support", "3 Work", "4 Comfort", "5 Utility" };
    private static readonly RoomDefinition.RoomCategory[] Categories =
    {
        RoomDefinition.RoomCategory.Housing,
        RoomDefinition.RoomCategory.LifeSupport,
        RoomDefinition.RoomCategory.Work,
        RoomDefinition.RoomCategory.Comfort,
        RoomDefinition.RoomCategory.Utility
    };

    private static readonly string[] RoomFiles =
    {
        "bunk_pod", "sky_loft", "air_recycler", "garden_nook",
        "workshop", "craft_lab", "star_lounge", "reading_nook",
        "storage_bay", "comm_relay"
    };

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private readonly Dictionary<RoomDefinition.RoomCategory, List<RoomDefinition>> _roomsByCategory = new();
    private int _activeTabIndex = 0;
    private RoomDefinition _selectedCardRoom;
    private bool _isDemolishActive;

    // -------------------------------------------------------------------------
    // UI nodes
    // -------------------------------------------------------------------------

    private VBoxContainer _vbox;
    private HBoxContainer _tabRow;
    private HBoxContainer _roomCardsContainer;
    private readonly List<Button> _tabButtons = new();
    private Button _demolishButton;
    private readonly List<PanelContainer> _roomCards = new();

    // Live cost preview label (positioned near ghost during placement)
    private Label _costLabel;

    // Refund preview label (positioned near hovered room during demolish)
    private Label _refundLabel;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Panel styling
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;

        var panelStyle = new StyleBoxFlat
        {
            BgColor = PanelBg,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 0,
            CornerRadiusBottomRight = 0,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 8,
            ContentMarginBottom = 8
        };
        AddThemeStyleboxOverride("panel", panelStyle);

        // Build UI hierarchy
        _vbox = new VBoxContainer();
        _vbox.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_vbox);

        BuildTabRow();
        BuildRoomCardsRow();
        BuildCostLabel();
        BuildRefundLabel();

        // Load room definitions
        LoadRoomDefinitions();

        // Subscribe to events
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.BuildModeChanged += OnBuildModeChanged;
            GameEvents.Instance.PlacementPreviewUpdated += OnPlacementPreviewUpdated;
            GameEvents.Instance.PlacementPreviewCleared += OnPlacementPreviewCleared;
            GameEvents.Instance.DemolishHoverUpdated += OnDemolishHoverUpdated;
            GameEvents.Instance.DemolishHoverCleared += OnDemolishHoverCleared;
            GameEvents.Instance.CreditsChanged += OnCreditsChanged;
            GameEvents.Instance.BlueprintUnlocked += OnBlueprintUnlocked;
        }

        // Default: select first tab (Housing)
        // Defer so room cards are built after all initialization
        CallDeferred(MethodName.SelectTab, 0);
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.BuildModeChanged -= OnBuildModeChanged;
            GameEvents.Instance.PlacementPreviewUpdated -= OnPlacementPreviewUpdated;
            GameEvents.Instance.PlacementPreviewCleared -= OnPlacementPreviewCleared;
            GameEvents.Instance.DemolishHoverUpdated -= OnDemolishHoverUpdated;
            GameEvents.Instance.DemolishHoverCleared -= OnDemolishHoverCleared;
            GameEvents.Instance.CreditsChanged -= OnCreditsChanged;
            GameEvents.Instance.BlueprintUnlocked -= OnBlueprintUnlocked;
        }
    }

    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed)
            return;

        // B: toggle panel
        if (key.Keycode == Key.B && !key.IsEcho())
        {
            Toggle();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Only handle further hotkeys when visible
        if (!Visible) return;

        // Escape: close panel + exit build mode
        if (key.Keycode == Key.Escape)
        {
            Hide();
            BuildManager.Instance?.ExitBuildMode();
            GetViewport().SetInputAsHandled();
            return;
        }

        // Keys 1-5: select category tab
        int tabIndex = key.Keycode switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            Key.Key5 => 4,
            _ => -1
        };

        if (tabIndex >= 0)
        {
            SelectTab(tabIndex);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        // Update cost label position to track ghost mesh each frame
        if (_costLabel.Visible && BuildManager.Instance != null
            && BuildManager.Instance.CurrentMode == BuildMode.Placing)
        {
            UpdateCostLabelPosition();
        }

        // Update refund label position when in demolish mode
        if (_refundLabel.Visible && BuildManager.Instance != null
            && BuildManager.Instance.CurrentMode == BuildMode.Demolish)
        {
            UpdateRefundLabelPosition();
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Toggle panel visibility. When showing, default to Housing tab.
    /// </summary>
    public void Toggle()
    {
        if (Visible)
        {
            Hide();
            BuildManager.Instance?.ExitBuildMode();
        }
        else
        {
            Show();
            _isDemolishActive = false;
            SelectTab(0);
        }
    }

    // -------------------------------------------------------------------------
    // UI Construction
    // -------------------------------------------------------------------------

    private void BuildTabRow()
    {
        _tabRow = new HBoxContainer();
        _tabRow.MouseFilter = MouseFilterEnum.Stop;
        _tabRow.AddThemeConstantOverride("separation", 4);
        _vbox.AddChild(_tabRow);

        // Category tab buttons
        for (int i = 0; i < CategoryNames.Length; i++)
        {
            var tabBtn = new Button();
            tabBtn.Text = CategoryNames[i];
            tabBtn.MouseFilter = MouseFilterEnum.Stop;
            tabBtn.FocusMode = FocusModeEnum.None;
            tabBtn.AddThemeFontSizeOverride("font_size", 14);
            tabBtn.CustomMinimumSize = new Vector2(110, 30);

            // Use category color for text
            Color catColor = RoomColors.GetCategoryColor(Categories[i]);
            tabBtn.AddThemeColorOverride("font_color", catColor);
            tabBtn.AddThemeColorOverride("font_hover_color", catColor.Lightened(0.2f));
            tabBtn.AddThemeColorOverride("font_pressed_color", catColor.Lightened(0.3f));

            // Style the button background
            var tabStyle = new StyleBoxFlat
            {
                BgColor = InactiveTabBg,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 4,
                ContentMarginBottom = 4
            };
            tabBtn.AddThemeStyleboxOverride("normal", tabStyle);
            tabBtn.AddThemeStyleboxOverride("hover", tabStyle);
            tabBtn.AddThemeStyleboxOverride("pressed", tabStyle);

            int capturedIndex = i;
            tabBtn.Pressed += () => SelectTab(capturedIndex);

            _tabButtons.Add(tabBtn);
            _tabRow.AddChild(tabBtn);
        }

        // Separator before demolish button
        var separator = new VSeparator();
        separator.CustomMinimumSize = new Vector2(20, 0);
        separator.MouseFilter = MouseFilterEnum.Ignore;
        _tabRow.AddChild(separator);

        // Demolish button
        _demolishButton = new Button();
        _demolishButton.Text = "Demolish";
        _demolishButton.MouseFilter = MouseFilterEnum.Stop;
        _demolishButton.FocusMode = FocusModeEnum.None;
        _demolishButton.AddThemeFontSizeOverride("font_size", 14);
        _demolishButton.AddThemeColorOverride("font_color", DemolishTint);
        _demolishButton.AddThemeColorOverride("font_hover_color", DemolishTint.Lightened(0.15f));
        _demolishButton.AddThemeColorOverride("font_pressed_color", DemolishTint.Lightened(0.3f));
        _demolishButton.CustomMinimumSize = new Vector2(100, 30);

        var demolishStyle = new StyleBoxFlat
        {
            BgColor = InactiveTabBg,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        _demolishButton.AddThemeStyleboxOverride("normal", demolishStyle);
        _demolishButton.AddThemeStyleboxOverride("hover", demolishStyle);
        _demolishButton.AddThemeStyleboxOverride("pressed", demolishStyle);

        _demolishButton.Pressed += OnDemolishButtonPressed;
        _tabRow.AddChild(_demolishButton);
    }

    private void BuildRoomCardsRow()
    {
        _roomCardsContainer = new HBoxContainer();
        _roomCardsContainer.MouseFilter = MouseFilterEnum.Stop;
        _roomCardsContainer.AddThemeConstantOverride("separation", 6);
        _roomCardsContainer.CustomMinimumSize = new Vector2(0, 70);
        _vbox.AddChild(_roomCardsContainer);
    }

    private void BuildCostLabel()
    {
        _costLabel = new Label();
        _costLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _costLabel.AddThemeColorOverride("font_color", Colors.White);
        _costLabel.AddThemeFontSizeOverride("font_size", 18);
        _costLabel.Visible = false;
        _costLabel.MouseFilter = MouseFilterEnum.Ignore;
        _costLabel.ZIndex = 100;
        // Add as direct child -- will use absolute positioning
        AddChild(_costLabel);
    }

    private void BuildRefundLabel()
    {
        _refundLabel = new Label();
        _refundLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _refundLabel.AddThemeColorOverride("font_color", RefundGreen);
        _refundLabel.AddThemeFontSizeOverride("font_size", 18);
        _refundLabel.Visible = false;
        _refundLabel.MouseFilter = MouseFilterEnum.Ignore;
        _refundLabel.ZIndex = 100;
        AddChild(_refundLabel);
    }

    // -------------------------------------------------------------------------
    // Room Loading
    // -------------------------------------------------------------------------

    private void LoadRoomDefinitions()
    {
        _roomsByCategory.Clear();

        foreach (var id in RoomFiles)
        {
            // Filter: only load unlocked rooms (locked rooms hidden, not greyed -- locked decision)
            if (HappinessManager.Instance != null && !HappinessManager.Instance.IsRoomUnlocked(id))
                continue;

            var def = ResourceLoader.Load<RoomDefinition>($"res://Resources/Rooms/{id}.tres");
            if (def != null)
            {
                if (!_roomsByCategory.ContainsKey(def.Category))
                    _roomsByCategory[def.Category] = new List<RoomDefinition>();
                _roomsByCategory[def.Category].Add(def);
            }
            else
            {
                GD.PushWarning($"BuildPanel: Failed to load room definition: {id}.tres");
            }
        }
    }

    // -------------------------------------------------------------------------
    // Tab Selection
    // -------------------------------------------------------------------------

    private void SelectTab(int tabIndex)
    {
        _activeTabIndex = tabIndex;
        _isDemolishActive = false;
        _selectedCardRoom = null;

        // Update tab button visuals
        for (int i = 0; i < _tabButtons.Count; i++)
        {
            var style = new StyleBoxFlat
            {
                BgColor = i == tabIndex ? ActiveTabBg : InactiveTabBg,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 4,
                ContentMarginBottom = 4
            };
            // Add a bottom border accent for active tab
            if (i == tabIndex)
            {
                Color catColor = RoomColors.GetCategoryColor(Categories[i]);
                style.BorderWidthBottom = 2;
                style.BorderColor = catColor;
            }
            _tabButtons[i].AddThemeStyleboxOverride("normal", style);
            _tabButtons[i].AddThemeStyleboxOverride("hover", style);
            _tabButtons[i].AddThemeStyleboxOverride("pressed", style);
        }

        // Reset demolish button to inactive style
        UpdateDemolishButtonStyle(false);

        // Populate room cards for selected category
        PopulateRoomCards(Categories[tabIndex]);

        // If switching from demolish mode, switch to placing the first room
        if (BuildManager.Instance != null && BuildManager.Instance.CurrentMode == BuildMode.Demolish)
        {
            // Just exit demolish mode; player can click a card to enter placing
            BuildManager.Instance.ExitBuildMode();
        }
    }

    // -------------------------------------------------------------------------
    // Room Cards
    // -------------------------------------------------------------------------

    private void PopulateRoomCards(RoomDefinition.RoomCategory category)
    {
        // Clear existing cards
        foreach (var card in _roomCards)
        {
            card.QueueFree();
        }
        _roomCards.Clear();

        if (!_roomsByCategory.TryGetValue(category, out var rooms))
            return;

        foreach (var room in rooms)
        {
            var card = CreateRoomCard(room);
            _roomCardsContainer.AddChild(card);
            _roomCards.Add(card);
        }
    }

    private PanelContainer CreateRoomCard(RoomDefinition room)
    {
        var card = new PanelContainer();
        card.MouseFilter = MouseFilterEnum.Stop;
        card.CustomMinimumSize = new Vector2(130, 60);

        Color catColor = RoomColors.GetCategoryColor(room.Category);

        // Card style with category color left border
        var cardStyle = new StyleBoxFlat
        {
            BgColor = CardBg,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
            BorderWidthLeft = 3,
            BorderColor = catColor,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 6,
            ContentMarginBottom = 6
        };
        card.AddThemeStyleboxOverride("panel", cardStyle);

        var vbox = new VBoxContainer();
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        card.AddChild(vbox);

        // Room name
        var nameLabel = new Label();
        nameLabel.Text = room.RoomName;
        nameLabel.AddThemeColorOverride("font_color", Colors.White);
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(nameLabel);

        // Cost estimate (1-segment outer row as base indicator)
        int baseCost = EconomyManager.Instance?.CalculateRoomCost(room, 1, true) ?? 0;
        bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.Credits >= baseCost;

        var costLabel = new Label();
        costLabel.Text = $"{baseCost} credits";
        costLabel.AddThemeColorOverride("font_color", canAfford ? MutedText : CostRedText);
        costLabel.AddThemeFontSizeOverride("font_size", 12);
        costLabel.MouseFilter = MouseFilterEnum.Ignore;
        costLabel.Name = "CostLabel"; // For refreshing on credit changes
        vbox.AddChild(costLabel);

        // Segment range
        var sizeLabel = new Label();
        sizeLabel.Text = room.MinSegments == room.MaxSegments
            ? $"{room.MinSegments} seg"
            : $"{room.MinSegments}-{room.MaxSegments} seg";
        sizeLabel.AddThemeColorOverride("font_color", MutedText);
        sizeLabel.AddThemeFontSizeOverride("font_size", 11);
        sizeLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(sizeLabel);

        // Store room definition as metadata for click handler
        card.SetMeta("room_def_id", room.RoomId);

        // Hover effect
        card.MouseEntered += () =>
        {
            if (_selectedCardRoom != room)
            {
                var hoverStyle = (StyleBoxFlat)cardStyle.Duplicate();
                hoverStyle.BgColor = CardHoverBg;
                card.AddThemeStyleboxOverride("panel", hoverStyle);
            }
        };
        card.MouseExited += () =>
        {
            if (_selectedCardRoom != room)
            {
                card.AddThemeStyleboxOverride("panel", cardStyle);
            }
        };

        // Click: enter placing mode
        card.GuiInput += (InputEvent @event) =>
        {
            if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
            {
                OnRoomCardClicked(room, card, cardStyle);
            }
        };

        return card;
    }

    private void OnRoomCardClicked(RoomDefinition room, PanelContainer card, StyleBoxFlat baseStyle)
    {
        _selectedCardRoom = room;
        _isDemolishActive = false;
        UpdateDemolishButtonStyle(false);

        // Highlight selected card, reset others
        foreach (var c in _roomCards)
        {
            string cardRoomId = c.GetMeta("room_def_id").AsString();
            if (cardRoomId == room.RoomId)
            {
                var selectedStyle = (StyleBoxFlat)baseStyle.Duplicate();
                selectedStyle.BgColor = CardSelectedBg;
                c.AddThemeStyleboxOverride("panel", selectedStyle);
            }
            else
            {
                // Reset to default card style (rebuild for simplicity)
                var resetStyle = new StyleBoxFlat
                {
                    BgColor = CardBg,
                    CornerRadiusTopLeft = 4,
                    CornerRadiusTopRight = 4,
                    CornerRadiusBottomLeft = 4,
                    CornerRadiusBottomRight = 4,
                    BorderWidthLeft = 3,
                    BorderColor = RoomColors.GetCategoryColor(room.Category),
                    ContentMarginLeft = 8,
                    ContentMarginRight = 8,
                    ContentMarginTop = 6,
                    ContentMarginBottom = 6
                };
                c.AddThemeStyleboxOverride("panel", resetStyle);
            }
        }

        BuildManager.Instance?.EnterPlacingMode(room);
    }

    // -------------------------------------------------------------------------
    // Demolish Button
    // -------------------------------------------------------------------------

    private void OnDemolishButtonPressed()
    {
        _isDemolishActive = true;
        _selectedCardRoom = null;

        // Deactivate tab highlight
        for (int i = 0; i < _tabButtons.Count; i++)
        {
            var style = new StyleBoxFlat
            {
                BgColor = InactiveTabBg,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 4,
                ContentMarginBottom = 4
            };
            _tabButtons[i].AddThemeStyleboxOverride("normal", style);
            _tabButtons[i].AddThemeStyleboxOverride("hover", style);
            _tabButtons[i].AddThemeStyleboxOverride("pressed", style);
        }

        UpdateDemolishButtonStyle(true);

        // Clear room cards when in demolish mode
        foreach (var card in _roomCards)
        {
            card.QueueFree();
        }
        _roomCards.Clear();

        BuildManager.Instance?.EnterDemolishMode();
    }

    private void UpdateDemolishButtonStyle(bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active ? DemolishActiveBg : InactiveTabBg,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
        if (active)
        {
            style.BorderWidthBottom = 2;
            style.BorderColor = DemolishTint;
        }
        _demolishButton.AddThemeStyleboxOverride("normal", style);
        _demolishButton.AddThemeStyleboxOverride("hover", style);
        _demolishButton.AddThemeStyleboxOverride("pressed", style);
    }

    // -------------------------------------------------------------------------
    // Live Cost Preview
    // -------------------------------------------------------------------------

    private void OnPlacementPreviewUpdated(int startFlatIndex, int segmentCount)
    {
        if (BuildManager.Instance == null) return;

        int cost = BuildManager.Instance.GetPlacementCost();
        bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.Credits >= cost;

        _costLabel.Text = $"-{cost}";
        _costLabel.AddThemeColorOverride("font_color",
            canAfford ? Colors.White : CostRedText);
        _costLabel.Visible = true;

        UpdateCostLabelPosition();
    }

    private void OnPlacementPreviewCleared()
    {
        _costLabel.Visible = false;
    }

    private void UpdateCostLabelPosition()
    {
        var camera = GetViewport().GetCamera3D();
        if (camera == null || BuildManager.Instance == null) return;

        Vector3 ghostWorldPos = BuildManager.Instance.GetGhostWorldPosition();
        if (ghostWorldPos == Vector3.Zero) return;

        Vector2 screenPos = camera.UnprojectPosition(ghostWorldPos);
        // Position above the ghost mesh
        _costLabel.Position = screenPos + new Vector2(-20, -40);
    }

    // -------------------------------------------------------------------------
    // Demolish Hover Preview
    // -------------------------------------------------------------------------

    private void OnDemolishHoverUpdated(int flatIndex, int refundAmount)
    {
        _refundLabel.Text = $"+{refundAmount}";
        _refundLabel.Visible = true;

        UpdateRefundLabelPosition();
    }

    private void OnDemolishHoverCleared()
    {
        _refundLabel.Visible = false;
    }

    private void UpdateRefundLabelPosition()
    {
        var camera = GetViewport().GetCamera3D();
        if (camera == null || BuildManager.Instance == null) return;

        // Use the ghost position API to find where to show refund
        // In demolish mode, we approximate using the hovered segment position
        Vector3 ghostWorldPos = BuildManager.Instance.GetGhostWorldPosition();
        if (ghostWorldPos != Vector3.Zero)
        {
            Vector2 screenPos = camera.UnprojectPosition(ghostWorldPos);
            _refundLabel.Position = screenPos + new Vector2(-20, -40);
        }
    }

    // -------------------------------------------------------------------------
    // Event Handlers
    // -------------------------------------------------------------------------

    private void OnBuildModeChanged(BuildMode mode)
    {
        switch (mode)
        {
            case BuildMode.Normal:
                _selectedCardRoom = null;
                _isDemolishActive = false;
                _costLabel.Visible = false;
                _refundLabel.Visible = false;
                // Reset card highlights
                RefreshCardHighlights();
                UpdateDemolishButtonStyle(false);
                break;

            case BuildMode.Placing:
                _isDemolishActive = false;
                UpdateDemolishButtonStyle(false);
                break;

            case BuildMode.Demolish:
                _isDemolishActive = true;
                _selectedCardRoom = null;
                UpdateDemolishButtonStyle(true);
                _costLabel.Visible = false;
                RefreshCardHighlights();
                break;
        }
    }

    private void OnCreditsChanged(int newBalance)
    {
        // Refresh room card cost affordability
        RefreshCardCosts();
    }

    /// <summary>
    /// Called when a new room blueprint is unlocked by HappinessManager.
    /// Re-loads all room definitions (now includes newly unlocked rooms)
    /// and refreshes the current tab to show new rooms immediately.
    /// Tab glow effect handled in Plan 02.
    /// </summary>
    private void OnBlueprintUnlocked(string roomType)
    {
        LoadRoomDefinitions();
        PopulateRoomCards(Categories[_activeTabIndex]);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void RefreshCardHighlights()
    {
        foreach (var card in _roomCards)
        {
            string cardRoomId = card.GetMeta("room_def_id").AsString();
            bool isSelected = _selectedCardRoom != null && _selectedCardRoom.RoomId == cardRoomId;

            Color catColor = RoomColors.GetCategoryColor(Categories[_activeTabIndex]);
            var style = new StyleBoxFlat
            {
                BgColor = isSelected ? CardSelectedBg : CardBg,
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4,
                CornerRadiusBottomRight = 4,
                BorderWidthLeft = 3,
                BorderColor = catColor,
                ContentMarginLeft = 8,
                ContentMarginRight = 8,
                ContentMarginTop = 6,
                ContentMarginBottom = 6
            };
            card.AddThemeStyleboxOverride("panel", style);
        }
    }

    private void RefreshCardCosts()
    {
        if (!_roomsByCategory.TryGetValue(Categories[_activeTabIndex], out var rooms))
            return;

        for (int i = 0; i < _roomCards.Count && i < rooms.Count; i++)
        {
            var room = rooms[i];
            var costLabel = _roomCards[i].FindChild("CostLabel", true, false) as Label;
            if (costLabel == null) continue;

            int baseCost = EconomyManager.Instance?.CalculateRoomCost(room, 1, true) ?? 0;
            bool canAfford = EconomyManager.Instance != null && EconomyManager.Instance.Credits >= baseCost;

            costLabel.Text = $"{baseCost} credits";
            costLabel.AddThemeColorOverride("font_color", canAfford ? MutedText : CostRedText);
        }
    }
}
