using Godot;
using OrbitalRings.UI;
using OrbitalRings.Autoloads;

namespace OrbitalRings.Ring;

/// <summary>
/// Mouse-to-segment detection using polar math (ray-plane intersection + Atan2).
/// Provides hover highlighting, click selection, and Escape deselection.
///
/// Extends SafeNode for future-proofing (currently emits events but doesn't consume them).
/// Attach as a child of RingVisual in Ring.tscn.
///
/// Picking strategy: NO physics/collision bodies. Uses Plane.IntersectsRay to find
/// the XZ intersection point, then polar math (distance + Atan2) to identify the
/// segment row and clock position.
///
/// Hover updates every frame in _Process to handle camera orbit invalidating
/// the hovered segment while the mouse is stationary.
/// </summary>
public partial class SegmentInteraction : OrbitalRings.Core.SafeNode
{
	private RingVisual _ringVisual;
	private Camera3D _camera;
	private SegmentTooltip _tooltip;

	/// <summary>
	/// Horizontal plane at ring surface height for ray intersection.
	/// The ring top face is at Y = RingHeight (0.3), so use half-height as the
	/// plane for best hit accuracy against the visible surface.
	/// </summary>
	private readonly Plane _ringPlane = new(Vector3.Up, SegmentGrid.RingHeight * 0.5f);

	/// <summary>Currently hovered segment flat index (0-23), -1 = none.</summary>
	private int _hoveredFlatIndex = -1;

	/// <summary>Currently selected segment flat index (0-23), -1 = none.</summary>
	private int _selectedFlatIndex = -1;

	/// <summary>Cached mouse position for _Process recalculation during camera orbit.</summary>
	private Vector2 _lastMousePos;

	// -------------------------------------------------------------------------
	// Lifecycle
	// -------------------------------------------------------------------------

	public override void _Ready()
	{
		base._Ready();

		// Parent is the RingVisual root node in Ring.tscn
		_ringVisual = GetParent<RingVisual>();

		// Get the active camera from the viewport
		_camera = GetViewport().GetCamera3D();

		// Find the SegmentTooltip in the scene tree (lives in CanvasLayer, not in Ring.tscn)
		_tooltip = GetTree().Root.FindChild("SegmentTooltip", true, false) as SegmentTooltip;
	}

	// -------------------------------------------------------------------------
	// Input
	// -------------------------------------------------------------------------

	public override void _Input(InputEvent @event)
	{
		// Mouse movement: cache position and update hover
		if (@event is InputEventMouseMotion mm)
		{
			_lastMousePos = mm.Position;
			UpdateHover();
		}
		// Left-click: select hovered segment
		else if (@event is InputEventMouseButton mb
				 && mb.ButtonIndex == MouseButton.Left
				 && mb.Pressed)
		{
			if (_hoveredFlatIndex >= 0)
			{
				SelectSegment(_hoveredFlatIndex);
				GetViewport().SetInputAsHandled();
			}
		}
		// Escape: deselect current segment
		else if (@event is InputEventKey key
				 && key.Pressed
				 && key.Keycode == Key.Escape)
		{
			DeselectSegment();
		}
	}

	// -------------------------------------------------------------------------
	// Process -- re-evaluate hover every frame for camera orbit safety
	// -------------------------------------------------------------------------

	public override void _Process(double delta)
	{
		// Always re-evaluate hover so camera orbit invalidates stale hover state
		// even when the mouse hasn't moved. This is cheap: one ray-plane
		// intersection + polar math per frame.
		UpdateHover();
	}

	// -------------------------------------------------------------------------
	// Polar math hover detection
	// -------------------------------------------------------------------------

	/// <summary>
	/// Casts a ray from camera through the cached mouse position, intersects
	/// with the ring plane, and converts to polar coordinates to identify
	/// which segment (if any) the mouse is over.
	/// </summary>
	private void UpdateHover()
	{
		if (_camera == null)
			return;

		// 1. Cast ray from camera through mouse position
		Vector3 rayOrigin = _camera.ProjectRayOrigin(_lastMousePos);
		Vector3 rayDir = _camera.ProjectRayNormal(_lastMousePos);

		// 2. Intersect with horizontal ring plane
		Vector3? hit = _ringPlane.IntersectsRay(rayOrigin, rayDir);
		if (hit == null)
		{
			ClearHover();
			return;
		}

		Vector3 hitPoint = hit.Value;

		// 3. Convert to polar coordinates
		float distance = Mathf.Sqrt(hitPoint.X * hitPoint.X + hitPoint.Z * hitPoint.Z);
		float angle = Mathf.Atan2(hitPoint.Z, hitPoint.X);

		// 4. Determine which row based on radial distance
		bool isOuter;
		SegmentRow row;

		if (distance >= SegmentGrid.OuterRowInner && distance <= SegmentGrid.OuterRadius)
		{
			isOuter = true;
			row = SegmentRow.Outer;
		}
		else if (distance >= SegmentGrid.InnerRadius && distance <= SegmentGrid.InnerRowOuter)
		{
			isOuter = false;
			row = SegmentRow.Inner;
		}
		else
		{
			// In walkway gap or outside ring entirely
			ClearHover();
			return;
		}

		// 5. Convert angle to segment clock position (0-11)
		if (angle < 0)
			angle += Mathf.Tau;

		int segIndex = (int)(angle / SegmentGrid.SegmentArc) % SegmentGrid.SegmentsPerRow;

		// 6. Compute flat index (0-23)
		int flatIndex = SegmentGrid.ToIndex(row, segIndex);

		// 7. Update hover state if changed
		if (flatIndex != _hoveredFlatIndex)
		{
			// Unhover old segment (restore to Normal unless it's the selected segment)
			if (_hoveredFlatIndex >= 0 && _hoveredFlatIndex != _selectedFlatIndex)
			{
				_ringVisual.SetSegmentState(_hoveredFlatIndex, SegmentVisualState.Normal);
			}

			_hoveredFlatIndex = flatIndex;

			// Apply hover visual (but don't downgrade a selected segment to hover)
			if (flatIndex != _selectedFlatIndex)
			{
				_ringVisual.SetSegmentState(flatIndex, SegmentVisualState.Hovered);
			}

			// Emit event
			GameEvents.Instance?.EmitSegmentHovered(segIndex, isOuter);
		}

		// 8. Update tooltip with current segment label
		string label = _ringVisual.Grid.GetLabel(row, segIndex);
		_tooltip?.Show(label, _lastMousePos);
	}

	/// <summary>
	/// Clears hover state, restoring the previously hovered segment to Normal
	/// (unless it's currently selected).
	/// </summary>
	private void ClearHover()
	{
		if (_hoveredFlatIndex >= 0)
		{
			if (_hoveredFlatIndex != _selectedFlatIndex)
			{
				_ringVisual.SetSegmentState(_hoveredFlatIndex, SegmentVisualState.Normal);
			}

			_hoveredFlatIndex = -1;
			GameEvents.Instance?.EmitSegmentUnhovered();
		}

		_tooltip?.Hide();
	}

	// -------------------------------------------------------------------------
	// Selection
	// -------------------------------------------------------------------------

	/// <summary>
	/// Selects the given segment, deselecting any previously selected segment.
	/// </summary>
	private void SelectSegment(int flatIndex)
	{
		if (flatIndex == _selectedFlatIndex)
			return; // Already selected, no-op

		// Deselect old segment
		if (_selectedFlatIndex >= 0)
		{
			// Restore old selected to Hovered if it's still hovered, otherwise Normal
			SegmentVisualState restoreState = (_selectedFlatIndex == _hoveredFlatIndex)
				? SegmentVisualState.Hovered
				: SegmentVisualState.Normal;
			_ringVisual.SetSegmentState(_selectedFlatIndex, restoreState);
		}

		// Select new segment
		_selectedFlatIndex = flatIndex;
		_ringVisual.SetSegmentState(flatIndex, SegmentVisualState.Selected);

		// Decompose flat index for event emission and logging
		var (row, pos) = SegmentGrid.FromIndex(flatIndex);
		bool isOuter = row == SegmentRow.Outer;

		GameEvents.Instance?.EmitSegmentSelected(pos, isOuter);
		GD.Print($"Selected: {_ringVisual.Grid.GetLabel(row, pos)}");
	}

	/// <summary>
	/// Deselects the currently selected segment.
	/// </summary>
	private void DeselectSegment()
	{
		if (_selectedFlatIndex < 0)
			return;

		// Restore to Hovered if mouse is still over it, otherwise Normal
		SegmentVisualState restoreState = (_selectedFlatIndex == _hoveredFlatIndex)
			? SegmentVisualState.Hovered
			: SegmentVisualState.Normal;
		_ringVisual.SetSegmentState(_selectedFlatIndex, restoreState);

		_selectedFlatIndex = -1;
		GameEvents.Instance?.EmitSegmentDeselected();
	}

	// -------------------------------------------------------------------------
	// SafeNode overrides (no-ops for now -- this node emits, doesn't consume)
	// -------------------------------------------------------------------------

	protected override void SubscribeEvents() { }
	protected override void UnsubscribeEvents() { }
}
