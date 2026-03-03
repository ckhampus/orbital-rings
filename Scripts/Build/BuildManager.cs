using System.Collections.Generic;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Data;
using OrbitalRings.Ring;

namespace OrbitalRings.Build;

/// <summary>
/// Central build state machine for room placement and demolition.
/// Manages Normal/Placing/Demolish modes, ghost previews, occupancy tracking,
/// and economy integration (spend on place, refund on demolish).
///
/// Registered as Autoload in project.godot -- access via BuildManager.Instance.
///
/// Placement flow:
///   1. EnterPlacingMode(room) -- player picks a room type
///   2. First click on free segment -- sets anchor, shows ghost preview
///   3. Hover on adjacent segments -- drag-to-resize ghost (1 to MaxSegments)
///   4. Second click -- confirm placement (spend credits, mark occupied, create block)
///   5. Escape at any point -- cancel current placement or exit build mode
///
/// Demolish flow:
///   1. EnterDemolishMode() -- player clicks demolish button
///   2. First click on placed room -- marks pending, shows confirm
///   3. Second click same room -- confirm demolish (refund credits, free segments)
///   4. Escape -- exit demolish mode
/// </summary>
public partial class BuildManager : Node
{
    /// <summary>Singleton instance, set in _Ready().</summary>
    public static BuildManager Instance { get; private set; }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private BuildMode _mode = BuildMode.Normal;
    private RoomDefinition _selectedRoom;
    private int _anchorFlatIndex = -1;
    private int _currentSize = 1;
    private int _startPos;
    private SegmentRow _anchorRow;
    private MeshInstance3D _ghostMesh;
    private int _pendingDemolishIndex = -1;

    // -------------------------------------------------------------------------
    // Room tracking
    // -------------------------------------------------------------------------

    private record PlacedRoom(
        RoomDefinition Definition,
        SegmentRow Row,
        int StartPos,
        int SegmentCount,
        int Cost,
        MeshInstance3D Mesh);

    private readonly Dictionary<int, PlacedRoom> _placedRooms = new();

    // -------------------------------------------------------------------------
    // References
    // -------------------------------------------------------------------------

    private RingVisual _ringVisual;

    // -------------------------------------------------------------------------
    // Public properties
    // -------------------------------------------------------------------------

    /// <summary>Current build mode.</summary>
    public BuildMode CurrentMode => _mode;

    /// <summary>Currently selected room definition (null when not in placing mode).</summary>
    public RoomDefinition SelectedRoom => _selectedRoom;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        Instance = this;

        // Find the Ring node's RingVisual in the scene tree
        _ringVisual = GetTree().Root.FindChild("Ring", true, false) as RingVisual;
        if (_ringVisual == null)
        {
            GD.PushWarning("BuildManager: RingVisual not found in scene tree. Build features disabled until Ring is present.");
        }

        // Instantiate PlacementFeedback as a child of BuildManager (Autoload)
        var feedback = new PlacementFeedback();
        feedback.Name = "PlacementFeedback";
        AddChild(feedback);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
        {
            HandleEscape();
            GetViewport().SetInputAsHandled();
        }
    }

    // -------------------------------------------------------------------------
    // Mode transitions
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enter placing mode with the given room type.
    /// Dims occupied segments for preventive feedback.
    /// </summary>
    public void EnterPlacingMode(RoomDefinition room)
    {
        if (_ringVisual == null) return;

        // Clean up any previous state
        ClearPlacementState();

        _mode = BuildMode.Placing;
        _selectedRoom = room;

        // Dim all occupied segments for preventive feedback (locked decision)
        DimOccupiedSegments();

        GameEvents.Instance?.EmitBuildModeChanged(_mode);
    }

    /// <summary>
    /// Enter demolish mode. Hovering placed rooms shows refund amounts.
    /// </summary>
    public void EnterDemolishMode()
    {
        if (_ringVisual == null) return;

        ClearPlacementState();

        _mode = BuildMode.Demolish;
        _selectedRoom = null;
        _pendingDemolishIndex = -1;

        GameEvents.Instance?.EmitBuildModeChanged(_mode);
    }

    /// <summary>
    /// Exit any build mode and return to Normal.
    /// Clears ghost preview, restores dimmed segments, clears pending demolish.
    /// </summary>
    public void ExitBuildMode()
    {
        ClearPlacementState();

        _mode = BuildMode.Normal;
        _selectedRoom = null;
        _pendingDemolishIndex = -1;

        // Restore all segments to Normal visual state
        UndimAllSegments();

        GameEvents.Instance?.EmitBuildModeChanged(_mode);
    }

    // -------------------------------------------------------------------------
    // Segment interaction
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called when a segment is clicked. Behavior depends on current build mode.
    /// </summary>
    public void OnSegmentClicked(int flatIndex)
    {
        if (_ringVisual == null) return;

        switch (_mode)
        {
            case BuildMode.Normal:
                // No-op -- let SegmentInteraction handle normal selection
                break;

            case BuildMode.Placing:
                HandlePlacingClick(flatIndex);
                break;

            case BuildMode.Demolish:
                HandleDemolishClick(flatIndex);
                break;
        }
    }

    /// <summary>
    /// Called when a segment is hovered. Updates ghost preview size or demolish hover.
    /// </summary>
    public void OnSegmentHovered(int flatIndex)
    {
        if (_ringVisual == null) return;

        switch (_mode)
        {
            case BuildMode.Placing when _anchorFlatIndex >= 0:
                HandlePlacingHover(flatIndex);
                break;

            case BuildMode.Demolish:
                HandleDemolishHover(flatIndex);
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Placement cost and room queries
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the cost for the current placement preview, or 0 if not in placing mode.
    /// </summary>
    public int GetPlacementCost()
    {
        if (_mode != BuildMode.Placing || _selectedRoom == null || _anchorFlatIndex < 0)
            return 0;

        return EconomyManager.Instance.CalculateRoomCost(
            _selectedRoom, _currentSize, _anchorRow == SegmentRow.Outer);
    }

    /// <summary>
    /// Returns the placed room at the given flat index, or null.
    /// Checks all placed rooms since a multi-segment room's anchor may differ from the queried index.
    /// </summary>
    public (RoomDefinition Definition, int AnchorIndex, int Cost)? GetPlacedRoom(int flatIndex)
    {
        // Direct anchor lookup
        if (_placedRooms.TryGetValue(flatIndex, out var directRoom))
            return (directRoom.Definition, flatIndex, directRoom.Cost);

        // Check if flatIndex falls within any multi-segment room's range
        var (queryRow, queryPos) = SegmentGrid.FromIndex(flatIndex);
        foreach (var (anchorIndex, room) in _placedRooms)
        {
            if (room.Row != queryRow) continue;

            for (int i = 0; i < room.SegmentCount; i++)
            {
                int pos = SegmentGrid.WrapPosition(room.StartPos + i);
                if (pos == queryPos)
                    return (room.Definition, anchorIndex, room.Cost);
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the demolish refund for the room at the given flat index, or 0.
    /// </summary>
    public int GetDemolishRefund(int flatIndex)
    {
        var roomInfo = GetPlacedRoom(flatIndex);
        if (roomInfo == null) return 0;

        return EconomyManager.Instance.CalculateDemolishRefund(roomInfo.Value.Cost);
    }

    /// <summary>
    /// Returns the ghost mesh's world position (used by BuildPanel for live cost label positioning).
    /// </summary>
    public Vector3 GetGhostWorldPosition()
    {
        return _ghostMesh?.GlobalPosition ?? Vector3.Zero;
    }

    // -------------------------------------------------------------------------
    // Placing mode handlers
    // -------------------------------------------------------------------------

    private void HandlePlacingClick(int flatIndex)
    {
        var (row, pos) = SegmentGrid.FromIndex(flatIndex);

        if (_anchorFlatIndex < 0)
        {
            // First click -- set anchor
            if (_ringVisual.Grid.IsOccupied(row, pos))
            {
                GameEvents.Instance?.EmitPlacementInvalid(flatIndex);
                return;
            }

            _anchorFlatIndex = flatIndex;
            _anchorRow = row;
            _startPos = pos;
            _currentSize = 1;

            // Create ghost preview
            _ghostMesh = RoomVisual.CreateRoomBlock(row, pos, 1, _selectedRoom, isGhost: true);
            _ringVisual.AddChild(_ghostMesh);

            GameEvents.Instance?.EmitPlacementPreviewUpdated(
                SegmentGrid.ToIndex(_anchorRow, _startPos), _currentSize);
        }
        else
        {
            // Confirm click -- validate and place
            if (!_ringVisual.Grid.AreSegmentsFree(_anchorRow, _startPos, _currentSize))
            {
                GameEvents.Instance?.EmitPlacementInvalid(flatIndex);
                return;
            }

            int cost = EconomyManager.Instance.CalculateRoomCost(
                _selectedRoom, _currentSize, _anchorRow == SegmentRow.Outer);

            if (!EconomyManager.Instance.TrySpend(cost))
            {
                GameEvents.Instance?.EmitPlacementInvalid(flatIndex);
                return;
            }

            // Mark segments occupied
            _ringVisual.Grid.SetSegmentsOccupied(_anchorRow, _startPos, _currentSize, true);

            // Remove ghost and create permanent room block
            RemoveGhost();
            var roomMesh = RoomVisual.CreateRoomBlock(
                _anchorRow, _startPos, _currentSize, _selectedRoom, isGhost: false);
            _ringVisual.AddChild(roomMesh);

            // Track placed room
            int anchorIndex = SegmentGrid.ToIndex(_anchorRow, _startPos);
            _placedRooms[anchorIndex] = new PlacedRoom(
                _selectedRoom, _anchorRow, _startPos, _currentSize, cost, roomMesh);

            // Emit events
            GameEvents.Instance?.EmitRoomPlaced(_selectedRoom.RoomId, anchorIndex);
            GameEvents.Instance?.EmitRoomPlacementConfirmed(
                roomMesh, roomMesh.GlobalPosition,
                RoomColors.GetCategoryColor(_selectedRoom.Category));

            // Dim newly occupied segments (player may continue placing)
            for (int i = 0; i < _currentSize; i++)
            {
                int dimPos = SegmentGrid.WrapPosition(_startPos + i);
                int dimIndex = SegmentGrid.ToIndex(_anchorRow, dimPos);
                _ringVisual.SetSegmentState(dimIndex, SegmentVisualState.Dimmed);
            }

            // Reset placement state but stay in Placing mode for repeated placement
            _anchorFlatIndex = -1;
            _currentSize = 1;

            GameEvents.Instance?.EmitPlacementPreviewCleared();
        }
    }

    private void HandlePlacingHover(int flatIndex)
    {
        var (row, pos) = SegmentGrid.FromIndex(flatIndex);

        // Must be same row as anchor
        if (row != _anchorRow) return;

        var (_, anchorPos) = SegmentGrid.FromIndex(_anchorFlatIndex);

        // Calculate delta with circular wrap
        int delta = pos - anchorPos;
        if (delta > 6) delta -= SegmentGrid.SegmentsPerRow;
        if (delta < -6) delta += SegmentGrid.SegmentsPerRow;

        // New size clamped to max segments
        int newSize = Mathf.Abs(delta) + 1;
        newSize = Mathf.Clamp(newSize, 1, _selectedRoom.MaxSegments);

        // Calculate start position (leftmost of anchor and hovered, accounting for wrap)
        int newStartPos;
        if (delta >= 0)
        {
            newStartPos = anchorPos;
        }
        else
        {
            // Dragging in negative direction
            newStartPos = SegmentGrid.WrapPosition(anchorPos - (newSize - 1));
        }

        // Only update if something changed
        if (newSize == _currentSize && newStartPos == _startPos)
            return;

        _currentSize = newSize;
        _startPos = newStartPos;

        // Remove old ghost and create new one
        RemoveGhost();
        _ghostMesh = RoomVisual.CreateRoomBlock(
            _anchorRow, _startPos, _currentSize, _selectedRoom, isGhost: true);

        // Check if all segments in new selection are free -- show invalid color if not
        if (!_ringVisual.Grid.AreSegmentsFree(_anchorRow, _startPos, _currentSize))
        {
            var invalidMat = new StandardMaterial3D
            {
                AlbedoColor = RoomColors.InvalidFlash,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha
            };
            _ghostMesh.MaterialOverride = invalidMat;
        }

        _ringVisual.AddChild(_ghostMesh);

        GameEvents.Instance?.EmitPlacementPreviewUpdated(
            SegmentGrid.ToIndex(_anchorRow, _startPos), _currentSize);
    }

    // -------------------------------------------------------------------------
    // Demolish mode handlers
    // -------------------------------------------------------------------------

    private void HandleDemolishClick(int flatIndex)
    {
        var roomInfo = GetPlacedRoom(flatIndex);
        if (roomInfo == null) return;

        int anchorIndex = roomInfo.Value.AnchorIndex;

        if (_pendingDemolishIndex == anchorIndex)
        {
            // Second click -- confirm demolish
            var room = _placedRooms[anchorIndex];

            int refund = EconomyManager.Instance.CalculateDemolishRefund(room.Cost);
            EconomyManager.Instance.Refund(refund);

            // Free segments
            _ringVisual.Grid.SetSegmentsOccupied(room.Row, room.StartPos, room.SegmentCount, false);

            // Emit feedback before removing mesh (feedback needs position)
            Vector3 meshPos = room.Mesh.GlobalPosition;
            Color roomColor = RoomColors.GetCategoryColor(room.Definition.Category);
            GameEvents.Instance?.EmitRoomDemolishConfirmed(meshPos, roomColor);

            // Remove mesh and tracking
            room.Mesh.QueueFree();
            _placedRooms.Remove(anchorIndex);

            GameEvents.Instance?.EmitRoomDemolished(anchorIndex);

            _pendingDemolishIndex = -1;
        }
        else
        {
            // First click -- mark pending
            _pendingDemolishIndex = anchorIndex;

            // Emit hover with refund amount for UI confirm popup
            int refund = EconomyManager.Instance.CalculateDemolishRefund(roomInfo.Value.Cost);
            GameEvents.Instance?.EmitDemolishHoverUpdated(anchorIndex, refund);
        }
    }

    private void HandleDemolishHover(int flatIndex)
    {
        var roomInfo = GetPlacedRoom(flatIndex);
        if (roomInfo != null)
        {
            int refund = EconomyManager.Instance.CalculateDemolishRefund(roomInfo.Value.Cost);
            GameEvents.Instance?.EmitDemolishHoverUpdated(flatIndex, refund);
        }
        else
        {
            GameEvents.Instance?.EmitDemolishHoverCleared();
        }
    }

    // -------------------------------------------------------------------------
    // Escape handling
    // -------------------------------------------------------------------------

    private void HandleEscape()
    {
        switch (_mode)
        {
            case BuildMode.Placing when _anchorFlatIndex >= 0:
                // Cancel current placement but stay in Placing mode
                ClearPlacementState();
                GameEvents.Instance?.EmitPlacementPreviewCleared();
                break;

            case BuildMode.Placing:
            case BuildMode.Demolish:
                ExitBuildMode();
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Dimming helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Dims all currently occupied segments for preventive feedback during placement mode.
    /// </summary>
    private void DimOccupiedSegments()
    {
        for (int i = 0; i < SegmentGrid.TotalSegments; i++)
        {
            var (row, pos) = SegmentGrid.FromIndex(i);
            if (_ringVisual.Grid.IsOccupied(row, pos))
            {
                _ringVisual.SetSegmentState(i, SegmentVisualState.Dimmed);
            }
        }
    }

    /// <summary>
    /// Restores all segments to Normal visual state.
    /// </summary>
    private void UndimAllSegments()
    {
        if (_ringVisual == null) return;

        for (int i = 0; i < SegmentGrid.TotalSegments; i++)
        {
            _ringVisual.SetSegmentState(i, SegmentVisualState.Normal);
        }
    }

    // -------------------------------------------------------------------------
    // Internal helpers
    // -------------------------------------------------------------------------

    private void ClearPlacementState()
    {
        RemoveGhost();
        _anchorFlatIndex = -1;
        _currentSize = 1;
    }

    private void RemoveGhost()
    {
        if (_ghostMesh != null)
        {
            _ghostMesh.QueueFree();
            _ghostMesh = null;
        }
    }
}
